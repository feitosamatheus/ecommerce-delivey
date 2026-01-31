(function ($) {
window.addLoading = function (text) {
    if (text != "" && text != null) {
        var textSpinner = document.querySelector(".text-spinner");
        textSpinner.textContent = text;
    }

    document.body.classList.add('loading');
}

window.removeLoading = function () {
    var textSpinner = document.querySelector(".text-spinner");

    document.body.classList.remove('loading');
    textSpinner.textContent = "Carregando...";
}



  function somaAcompanhamentos($modal) {
    debugger
    const selecionados = $modal.find('.acomp-input:checked').toArray().map(el => {
        const raw = (el.dataset.preco ?? '0').toString().trim();
        // segurança: aceita "1,90" e "1.90"
        const preco = parseFloat(raw.replace(',', '.')) || 0;

        return {
        id: el.value,
        categoriaId: el.dataset.categoriaId,
        raw,
        preco
        };
    });

    console.table(selecionados);

    return selecionados.reduce((acc, x) => acc + x.preco, 0);
    }


    function atualizarTotalModal($modal) {
        const precoBaseRaw = ($modal.find('#modalPrecoBase').val() || '0').toString().trim();
        const precoBase = parseFloat(precoBaseRaw.replace(',', '.')) || 0;

        const qtd = parseInt($modal.find('#modalProdutoQtd').val() || '1', 10) || 1;

        const somaAcomp = somaAcompanhamentos($modal);

        const total = (precoBase * qtd ) + somaAcomp;

        $modal.find('#totalModal').text(
            total.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
        );
    }

  // + / -
  $(document)
    .off('click.modalQtd')
    .on('click.modalQtd', '#modalHost .btn-qtd', function (e) {
      e.preventDefault();

      const $btn = $(this);
      const delta = parseInt($btn.data('delta'), 10);

      const $modal = $btn.closest('#modalAddProduto');
      if ($modal.length === 0) return;

      const $qtd = $modal.find('#modalProdutoQtd');
      let atual = parseInt($qtd.val() || '1', 10);
      if (isNaN(atual)) atual = 1;

      const novo = atual + delta;
      if (novo >= 1) {
        $qtd.val(novo);
        atualizarTotalModal($modal);
      }
    });

  // ACOMPANHAMENTOS (radio/checkbox)
  $(document)
    .off('change.modalAcomp')
    .on('change.modalAcomp', '#modalHost .acomp-input', function () {
      const $modal = $(this).closest('#modalAddProduto');
      if ($modal.length === 0) return;

      atualizarTotalModal($modal);
    });

  // Ao abrir o modal
  $(document)
    .off('shown.bs.modal.modalTot')
    .on('shown.bs.modal.modalTot', '#modalAddProduto', function () {
      atualizarTotalModal($(this));
    });

    function getModalPayload($modal) {
    const produtoId = $modal.find('#modalProdutoId').val();
    const qtd = parseInt($modal.find('#modalProdutoQtd').val() || '1', 10) || 1;
    const obs = ($modal.find('#modalProdutoObs').val() || '').trim();

    // acompanhamentos selecionados (agrupados por categoria)
    const acompanhamentos = $modal.find('.acomp-input:checked').toArray().map(el => ({
      acompanhamentoId: el.value,
      categoriaId: el.dataset.categoriaId
    }));

    return { produtoId, quantidade: qtd, observacao: obs, acompanhamentos };
  }

  function setLoadingAdicionar($modal, isLoading) {
    const $btn = $modal.find('#btnConfirmarAddProduto');
    $btn.prop('disabled', isLoading);

    if (isLoading) {
      $btn.data('oldHtml', $btn.html());
      $btn.html('<span>Adicionando...</span><span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>');
    } else {
      const old = $btn.data('oldHtml');
      if (old) $btn.html(old);
    }
  }

  function renderCarrinho(html) {
    // Ajuste o seletor para o seu container real do carrinho
    $('#cartHost').html(html);
  }

  // Clique em "Adicionar"
  // Clique em "Adicionar"
$(document)
  .off('click.modalAddProduto')
  .on('click.modalAddProduto', '#modalHost #btnConfirmarAddProduto', async function (e) {
    e.preventDefault();

    const $modal = $(this).closest('#modalAddProduto');
    if ($modal.length === 0) return;

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

      // tenta ler JSON mesmo no erro
      const data = await resp.json().catch(() => null);

      if (!resp.ok) {
        const msg = data?.message || `Erro HTTP ${resp.status}`;
        throw new Error(msg);
      }

      // fecha modal
      const modalEl = $modal.get(0);
      const instance = bootstrap.Modal.getInstance(modalEl) || bootstrap.Modal.getOrCreateInstance(modalEl);
      instance.hide();

      alert(data?.message || 'Adicionado ao carrinho com sucesso!');

      // redireciona para home e força atualização do carrinho lá
      window.location.href = '/';

    } catch (err) {
      console.error(err);
      const $msg = $modal.find('#modalAddProdutoMsg');
      if ($msg.length) $msg.removeClass('d-none').text(err.message || 'Erro ao adicionar.');
      else alert(err.message || 'Não foi possível adicionar ao carrinho.');
    } finally {
      setLoadingAdicionar($modal, false);
    }
  });


})(jQuery);


