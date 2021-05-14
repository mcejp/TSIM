// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// code stolen from wherever

function refreshCameras() {
  $('img.auto-refresh').attr('src', function(i, old) { return old.replace(/\?.+/,"?i=" + (Math.random()*1000)); });
  setTimeout(refreshCameras, 1000);
}
function refreshCamerasFirst() {
  $('img.auto-refresh').attr('src', function(i, old) { return old + "?i=" + (Math.random()*1000); });
  setTimeout(refreshCameras, 1000);
}
$(function() {
    setTimeout(refreshCamerasFirst, 1000);

    /*
    $('#unit-0-speed-set').click(() => {
        const data = {'unit': 0, 'speed': Number.parseFloat($('#unit-0-speed').val())};
        $.ajax({
        type: "POST",
        url:'@Url.Action("UnitSpeedSet", "Home")',
        data: JSON.stringify(data),
        contentType: 'application/json'
        });
    });
    */
});
