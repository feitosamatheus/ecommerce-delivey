(function ($) {
    // ===========================
    // Loading helpers (globais)
    // ===========================
    window.addLoading = function (text) {
        if (text != "" && text != null) {
            var textSpinner = document.querySelector(".text-spinner");
            if (textSpinner) textSpinner.textContent = text;
        }
        document.body.classList.add('loading');
    };

    window.removeLoading = function () {
        var textSpinner = document.querySelector(".text-spinner");
        document.body.classList.remove('loading');
        if (textSpinner) textSpinner.textContent = "Carregando...";
    };

    // ===========================
    // Constantes
    // ===========================
    const MODAL_ID = 'modalAddProduto';
    const MODAL_SEL = `#${MODAL_ID}`;
    const BTN_CONFIRM_ID = 'btnConfirmarInclusaoProduto'; // ✅ novo id
    const BTN_CONFIRM_SEL = `#${BTN_CONFIRM_ID}`;

    // ===========================
    // Utils
    // ===========================
    function parseMoney(v) {
        const s = (v ?? '0').toString().trim().replace(',', '.');
        const n = parseFloat(s);
        return isNaN(n) ? 0 : n;
    }

    function formatBRL(v) {
        return (v || 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
    }

    function getModal() {
        return $(MODAL_SEL);
    }

    function getModalEl() {
        return document.getElementById(MODAL_ID);
    }

    function hideAddProdutoModal() {
        const el = getModalEl();
        if (!el) return;
        const instance = bootstrap.Modal.getInstance(el) || bootstrap.Modal.getOrCreateInstance(el);
        instance.hide();
    }

    // ===========================
    // Acompanhamentos / Total
    // ===========================
    function getSelectedAcomps($modal) {
        return $modal.find('.acomp-input:checked').toArray().map(el => ({
            id: el.value,
            categoriaId: el.dataset.categoriaId,
            preco: parseMoney(el.dataset.preco)
        }));
    }

    // ✅ total = (base + extras) * qtd
    function atualizarTotalModal($modal) {
        const precoBase = parseMoney($modal.find('#modalPrecoBase').val());
        const qtd = parseInt($modal.find('#modalProdutoQtd').val() || '1', 10) || 1;

        const selecionados = getSelectedAcomps($modal);
        const somaExtras = selecionados.reduce((acc, x) => acc + x.preco, 0);

        const total = (precoBase + somaExtras) * qtd;

        $modal.find('#totalModal').text(formatBRL(total));
    }

    // ===========================
    // Regras por categoria (min/max/obrigatório)
    // ===========================
    function enforceMaxForCategory($cat) {
        const max = parseInt($cat.data('max'), 10) || 0;
        if (max <= 0) return;

        const $checked = $cat.find('.acomp-input:checked');
        if ($checked.length > max) {
            // desmarca o último marcado
            $checked.last().prop('checked', false);
        }
    }

    function validateCategories($modal) {
        let ok = true;

        $modal.find('[data-acomp-categoria]').each(function () {
            const $cat = $(this);
            const catId = $cat.data('categoria-id');

            const min = parseInt($cat.data('min'), 10) || 0;
            const obrigatorio = ('' + $cat.data('obrigatorio')) === '1';

            const qtdSel = $cat.find('.acomp-input:checked').length;
            const precisaMin = obrigatorio || min > 0;

            const $err = $modal.find(`[data-acomp-erro="${catId}"]`);

            if (precisaMin && qtdSel < min) {
                ok = false;
                $err.removeClass('d-none');
            } else {
                $err.addClass('d-none');
            }
        });

        return ok;
    }

    function scrollToFirstError($modal) {
        const $firstErr = $modal.find('[data-acomp-erro]:not(.d-none)').first();
        if (!$firstErr.length) return;

        // se você estiver usando scroll apenas no painel de detalhes, ele deve ter a classe details-scroll
        const $scroll = $modal.find('.details-scroll').length ? $modal.find('.details-scroll') : $modal.find('.modal-body');
        $scroll.animate({ scrollTop: $scroll.scrollTop() + $firstErr.position().top - 24 }, 200);
    }

    // ===========================
    // Payload
    // ===========================
    function getModalPayload($modal) {
        const produtoId = $modal.find('#modalProdutoId').val();
        const qtd = parseInt($modal.find('#modalProdutoQtd').val() || '1', 10) || 1;
        const obs = ($modal.find('#modalProdutoObs').val() || '').trim();

        const acompanhamentos = $modal.find('.acomp-input:checked').toArray().map(el => ({
            acompanhamentoId: el.value,
            categoriaId: el.dataset.categoriaId
        }));

        return { produtoId, quantidade: qtd, observacao: obs, acompanhamentos };
    }

    function setLoadingAdicionar($modal, isLoading) {
        const $btn = $modal.find(BTN_CONFIRM_SEL);
        if (!$btn.length) return;

        $btn.prop('disabled', isLoading);

        if (isLoading) {
            $btn.data('oldHtml', $btn.html());
            $btn.html('<span>Adicionando...</span><span class="spinner-border spinner-border-sm ms-2" role="status" aria-hidden="true"></span>');
        } else {
            const old = $btn.data('oldHtml');
            if (old) $btn.html(old);
        }
    }

    // ===========================
    // Eventos: quantidade +/-
    // ===========================
    $(document)
        .off('click.modalQtd')
        .on('click.modalQtd', '#modalHost .btn-qtd', function (e) {
            e.preventDefault();

            const $btn = $(this);
            const delta = parseInt($btn.data('delta'), 10) || 0;

            const $modal = getModal();
            if ($modal.length === 0) return;

            const $qtd = $modal.find('#modalProdutoQtd');
            let atual = parseInt($qtd.val() || '1', 10);
            if (isNaN(atual) || atual < 1) atual = 1;

            const novo = atual + delta;
            if (novo >= 1) {
                $qtd.val(novo);
                atualizarTotalModal($modal);
            }
        });

    // ===========================
    // Eventos: acompanhamentos (enforce max + total + validação)
    // ===========================
    $(document)
        .off('change.modalAcomp')
        .on('change.modalAcomp', '#modalHost .acomp-input', function () {
            const $modal = getModal();
            if ($modal.length === 0) return;

            const $cat = $(this).closest('[data-acomp-categoria]');
            enforceMaxForCategory($cat);

            atualizarTotalModal($modal);
            validateCategories($modal);
        });

    // ===========================
    // Ao abrir o modal
    // ===========================
    $(document)
        .off('shown.bs.modal.modalTot')
        .on('shown.bs.modal.modalTot', MODAL_SEL, function () {
            const $modal = $(this);
            atualizarTotalModal($modal);
            validateCategories($modal);
        });

    // ===========================
    // Clique em "Adicionar" (novo id)
    // ===========================
    $(document)
        .off('click.modalAddProduto')
        .on('click.modalAddProduto', `#modalHost ${BTN_CONFIRM_SEL}`, async function (e) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();

            const $modal = getModal();
            if ($modal.length === 0) return;

            if (!validateCategories($modal)) {
                scrollToFirstError($modal);
                return;
            }

            const payload = getModalPayload($modal);
            if (!payload.produtoId) return;

            setLoadingAdicionar($modal, true);

            try {
                const resp = await fetch('/Carrinho/Adicionar', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: JSON.stringify(payload)
                });

                const data = await resp.json().catch(() => null);

                if (!resp.ok) {
                    const msg = data?.message || `Erro HTTP ${resp.status}`;
                    throw new Error(msg);
                }

                // ✅ fecha modal pelo ID (Bootstrap 5)
                hideAddProdutoModal();

                alert(data?.message || 'Adicionado ao carrinho com sucesso!');

                // ⚠️ Evita gatilhos de auto-open de outros modais
                window.location.href = '/';

            } catch (err) {
                console.error(err);
                alert(err?.message || 'Não foi possível adicionar ao carrinho.');
            } finally {
                setLoadingAdicionar($modal, false);
            }
        });

})(jQuery);
