$(document).ready(function () {
    $('.sign-in-textbox').focus(function () {
        $(this).prev().addClass('active');
    });
    $('.sign-in-textbox').blur(function () {
        $(this).prev().removeClass('active');
    });
});