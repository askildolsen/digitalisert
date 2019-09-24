import React, { useEffect, useState } from 'react';
import { Map, Marker, Polygon, Popup, TileLayer, ZoomControl } from 'react-leaflet';
import L from 'leaflet';
import Wkt from 'wicket';

function ResourcePopup({ resource }) {
  return (
    <Popup>
      <small className="text-muted">{resource.code}</small>
      <span> - </span>
      {resource.title}
    </Popup>
  );
}

function App({url}) {

  const [bounds, setBounds] = useState();
  const [center, setCenter] = useState();
  const [markers, setMarkers] = useState();

  useEffect(() => {
    const fetchData = async (url) => {
      const resources = await (await fetch(url)).json();
      const responsemarkers =
        resources.map((resource, rindex) => {
          return resource.properties.filter(p => p.tags.includes("@wkt")).map((property, pindex) => {
            return property.value.map((value, vindex) => {
              var wkt = new Wkt.Wkt().read(value);
              if (wkt.type === "point") {
                return (
                  <Marker key={rindex + "-" + pindex + "-" + vindex} position={[wkt.components[0].y, wkt.components[0].x]} icon={L.divIcon()}>
                    <ResourcePopup resource={resource}/>
                  </Marker>
                )
              } else {
                return (
                  <Polygon key={rindex + "-" + pindex + "-" + vindex} positions={wkt.components[0].map(c => { return [c.y, c.x] } )}>
                    <ResourcePopup resource={resource}/>
                  </Polygon>
                )
              }
            });
          });
        }).reduce((x,y) => x.concat(y), []).reduce((x,y) => x.concat(y), []);

        const positions = [].concat(...responsemarkers.map(m => (m.props.position !== undefined) ? [m.props.position] : m.props.positions));
        if (positions.length > 1) {
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
