$(".map").each(function() {
    var wkt = new Wkt.Wkt().read($(this).text());
    $(this).empty();

    var polygon = (wkt.type == "point") ? L.circle([wkt.components[0].y, wkt.components[0].x], { radius: 3 }) : wkt.toObject();
    
    var map = L.map($(this).get(0)).setView([60.14, 10.25], 11);
    polygon.addTo(map);
    map.fitBounds(polygon.getBounds());

    L.tileLayer('https://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=topo4&zoom={z}&x={x}&y={y}', {
        attribution: '<a href="http://www.kartverket.no/">Kartverket</a>'
    }).addTo(map);
});
