import React, { useEffect, useState } from 'react';
import { Map, Marker, Polygon, Popup, TileLayer, ZoomControl } from 'react-leaflet';
import L from 'leaflet';
import Wkt from 'wicket';

function ResourcePopup({ resource } : any) {
  return (
    <Popup>
      <small className="text-muted">{resource.code}</small>
      <span> - </span>
      {resource.title}
    </Popup>
  );
}

function App({url} : any) {

  const [bounds, setBounds] = useState<Array<[number, number]> | undefined>();
  const [center, setCenter] = useState();
  const [markers, setMarkers] = useState();

  useEffect(() => {
    const fetchData = async (url: string) => {
      const resources = await (await fetch(url)).json();
      const responsemarkers =
        resources.map((resource: any, rindex: Number) => {
          return resource.properties.filter((p: any) => p.tags.includes("@wkt")).map((property: any, pindex: Number) => {
            return property.value.map((value: string, vindex: Number) => {
              var wkt = new Wkt.Wkt().read(value);
              if (wkt.type === "point") {
                return (
                  <Marker key={rindex + "-" + pindex + "-" + vindex} position={[wkt.components[0].y, wkt.components[0].x]} icon={L.divIcon()}>
                    <ResourcePopup resource={resource}/>
                  </Marker>
                )
              } else {
                const polygonpositions = (wkt.components.length > 1)
                  ? wkt.components.map((co: any) => { return co.map((c: any) => { return [c.y, c.x] } ) })
                  : wkt.components[0].map((c: any) => { return [c.y, c.x] } );
                return (
                  <Polygon key={rindex + "-" + pindex + "-" + vindex} positions={ polygonpositions }>
                    <ResourcePopup resource={resource}/>
                  </Polygon>
                )
              }
            });
          });
        }).reduce((x: any,y: any) => x.concat(y), []).reduce((x: any, y: any) => x.concat(y), []);

        const positions = [].concat(...responsemarkers.map((m: any) => (m.props.position) ? [m.props.position] : m.props.positions));
        if (positions.length > 1 && positions.some((p1: any) => positions.some((p2: any) => p1.join('|') !== p2.join('|')))) {
          setBounds(positions);
        }
        else {
          setCenter(positions[0]);
        }

        setMarkers(responsemarkers);
    }

    fetchData(url);
  }, [url]);

  return (
    <div className="embed-responsive embed-responsive-16by9">
      <Map bounds={bounds} center={center} zoom={11} className="embed-responsive-item" zoomControl={false}>
        <TileLayer
          url="https://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=topo4&zoom={z}&x={x}&y={y}"
          attribution="<a href='http://www.kartverket.no'>Kartverket</a>"
        />
        {markers}
        <ZoomControl position="topright" />
      </Map>
    </div>
  );
}

export default App;
