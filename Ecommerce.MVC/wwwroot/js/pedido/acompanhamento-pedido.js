function copyToClipboard(elementId) {
    var copyText = document.getElementById(elementId);
    copyText.select();
    copyText.setSelectionRange(0, 99999); // Para dispositivos móveis

    navigator.clipboard.writeText(copyText.value).then(() => {
        alert("Código PIX copiado com sucesso!");
    }).catch(err => {
        console.error('Erro ao copiar: ', err);
    });
}

$(function () {
    $('.toggle-detalhes').each(function () {
        const $btn = $(this);
        const targetId = $btn.data('target');
        const el = document.getElementById(targetId);

        const $icon = $btn.find('.fa-chevron-down, .fa-chevron-up');
        const collapse = new bootstrap.Collapse(el, { toggle: false });

        $btn.on('click', function (e) {
            e.preventDefault();

            const aberto = $(el).hasClass('show');
            if (aberto) {
                collapse.hide();
                $icon.removeClass('fa-chevron-up').addClass('fa-chevron-down');
            } else {
                collapse.show();
                $icon.removeClass('fa-chevron-down').addClass('fa-chevron-up');
            }
        });
    });

});