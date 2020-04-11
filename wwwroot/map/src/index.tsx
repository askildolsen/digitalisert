import React from 'react';
import ReactDOM from 'react-dom';
import App from './App';
import 'leaflet/dist/leaflet.css';

var root = document.getElementById('map');

if (root) {
    ReactDOM.render(<App { ...(root.dataset) }/>, root);
}
