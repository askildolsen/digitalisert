$(".map").each(function() {
    var wkt = new Wkt.Wkt();
    var polygon = wkt.read($(this).text()).toObject();
    $(this).empty();
    var map = L.map($(this).get(0));
    L.tileLayer('https://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=topo4&zoom={z}&x={x}&y={y}', {
        attribution: '<a href="http://www.kartverket.no/">Kartverket</a>'
    }).addTo(map);
    polygon.addTo(map);
    map.fitBounds(polygon.getBounds());
});
