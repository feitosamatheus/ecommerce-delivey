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