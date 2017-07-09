function changeAvatarSource()
{
    $('.avatar-section').hide();
    var src = $('#lstAvatarResources').val();
    if (src === 'LocalStorage') {
        $('#uploadAvatar').show();
    } else if (src === 'WeChatPolling') {
        $('#wechatAvatar').show();
    } else {
        $('#gravatar').show();
    }
}

$(document).ready(function () {
    changeAvatarSource();
    $('#lstAvatarResources').change(function () {
        changeAvatarSource();
    });
});

function accountChangeApplicationAuthorization(openId, isDisabled)
{
    $('#frmAuthorization').submit();
}