// Funcionalidad para el toggle del sidebar
$("#menu-toggle").click(function (e) {
    e.preventDefault();
    $("#wrapper").toggleClass("toggled");
});

// Función para inicializar los submenús desplegables
function initMenu() {
    $('#menu ul').hide(); // Ocultar todos los submenús por defecto
    
    // Mostrar submenú si el item actual está activo
    $('#menu ul').children('.current').parent().show(); 

    $('#menu li a').click(
        function () {
            var checkElement = $(this).next();
            if ((checkElement.is('ul')) && (checkElement.is(':visible'))) {
                // Si el submenú está visible, lo oculta
                checkElement.slideUp('normal');
                return false;
            }
            if ((checkElement.is('ul')) && (!checkElement.is(':visible'))) {
                // Si el submenú está oculto, oculta los demás y muestra este
                $('#menu ul:visible').slideUp('normal');
                checkElement.slideDown('normal');
                return false;
            }
        }
    );
}

// Ejecutar la función cuando el documento esté listo
$(document).ready(function () {
    initMenu();
});