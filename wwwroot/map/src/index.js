import React from 'react';
import ReactDOM from 'react-dom';
import App from './App';
import 'leaflet/dist/leaflet.css';

var root = document.getElementById('map');

ReactDOM.render(<App url={root.dataset.url}/>, root);
