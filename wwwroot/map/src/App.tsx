import React, { useEffect, useState, CSSProperties } from 'react';
import { Map, MapLayer, Marker, LayersControl, LayerGroup, Polygon, ScaleControl, TileLayer, Tooltip } from 'react-leaflet';
import L from 'leaflet';
import Wkt from 'wicket';

function App({resource, resources} : any) {

  const [bounds, setBounds] = useState<Array<[number, number]> | undefined>();
  const [center, setCenter] = useState();
  const [primaryMapLayer, setPrimaryMapLayer] = useState<MapLayer[]>([]);
  const [seondaryMapLayer, setSeondaryMapLayer] = useState<MapLayer[]>([]);

  useEffect(() => {
    const fetchResourceData = async (resources: string[], primary : boolean) => {
      const resourceData = await Promise.all(resources.map(r => fetch("/api/Resource/" + r).then(r => r.json())));
      const fillOpacity = (primary) ? 0.2 : 0.1;
      const responsecomponents =
        [].concat(...resourceData).flatMap((resource: any, rindex: Number) => {
          return resource.properties.filter((p: any) => p.tags.includes("@wkt")).flatMap((property: any, pindex: Number) => {
            return property.value.flatMap((value: string, vindex: Number) => {
              const wkt = new Wkt.Wkt().read(value);
              if (wkt.type === "point") {
                return [
                  <Marker key={rindex + "-" + pindex + "-" + vindex} position={[wkt.components[0].y, wkt.components[0].x]} icon={L.divIcon()}>
                    <Tooltip>{resource.title}</Tooltip>
                  </Marker>
                ]
              } else {
                const polygonpositions = (wkt.components.length > 1)
                  ? wkt.components.map((co: any) => { return co.map((c: any) => { return [c.y, c.x] } ) })
                  : wkt.components[0].map((c: any) => { return [c.y, c.x] } );
                return [
                  <Polygon key={rindex + "-" + pindex + "-" + vindex} positions={ polygonpositions } fillOpacity={fillOpacity}>
                    <Tooltip>{resource.title}</Tooltip>
                  </Polygon>
                ]
              }
            });
          });
        });

        if (primary) {
          const positions = [].concat(...responsecomponents.map((m: any) => (m.props.position) ? [m.props.position] : m.props.positions));
          if (positions.length > 1 && positions.some((p1: any) => positions.some((p2: any) => p1.join('|') !== p2.join('|')))) {
            setBounds(positions);
          }
          else {
            setCenter(positions[0]);
          }

          setPrimaryMapLayer(responsecomponents);
        } else {
          setSeondaryMapLayer(responsecomponents);
        }
    }

    if (resource) {
      fetchResourceData( resources, false);
      fetchResourceData( [resource], true);
    } else {
      fetchResourceData( resources, true);
    }
  }, [resource, resources]);

  const styles : CSSProperties = { position: 'absolute', top: 0, bottom:'0', width: '100%' };

  return (
      <Map bounds={bounds} center={center} zoom={11} scrollWheelZoom={false} touchZoom={false} style={styles}>
        <TileLayer
          attribution='&copy; <a href="http://osm.org/copyright">OpenStreetMap</a> contributors'
          url="https://a.tile.openstreetmap.org/{z}/{x}/{y}.png "
        />
        <LayersControl position="topright">
          <LayersControl.BaseLayer name="Topologisk Norgeskart" checked={true}>
            <LayerGroup>
              <TileLayer
                url="https://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=topo4&zoom={z}&x={x}&y={y}"
                attribution="<a href='http://www.kartverket.no'>Kartverket</a>"
              />
            </LayerGroup>
          </LayersControl.BaseLayer>
          <LayersControl.BaseLayer name="Topologisk Norgeskart med Europa">
            <LayerGroup>
              <TileLayer
                url="https://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=europa&zoom={z}&x={x}&y={y}"
                attribution="<a href='http://www.kartverket.no'>Kartverket</a>"
              />
              <TileLayer
                url="https://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=topo4&zoom={z}&x={x}&y={y}"
                attribution="<a href='http://www.kartverket.no'>Kartverket</a>"
              />
            </LayerGroup>
          </LayersControl.BaseLayer>
          {seondaryMapLayer.length &&
            <LayersControl.Overlay name="References" checked={false}>
              <LayerGroup>
                {seondaryMapLayer}
              </LayerGroup>
            </LayersControl.Overlay>
          }
        </LayersControl>
        {primaryMapLayer}
        <ScaleControl/>
      </Map>
  );
}

export default App;
