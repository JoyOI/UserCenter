$(document).ready(function () {
    $('.textbox').focus(function () {
        $(this).prev().addClass('active');
    });
    $('.textbox').blur(function () {
        $(this).prev().removeClass('active');
    });
});