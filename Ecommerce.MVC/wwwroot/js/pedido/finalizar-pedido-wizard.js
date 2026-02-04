(function () {
    const MODAL_ID = "modalFinalizarPedido";
    const BODY_ID = "modalFinalizarPedidoBody";

    const titles = { 1: "Recebimento do Pedido", 2: "Confirmação do pedido", 3: "Pagamento" };
    let step = 1;

    // -----------------------------
    // Helpers
    // -----------------------------
    function root() { return document.getElementById(MODAL_ID); }
    function qs(sel) { return root()?.querySelector(sel); }
    function qsa(sel) { return root()?.querySelectorAll(sel) ?? []; }
    function body() { return document.getElementById(BODY_ID); }

    function showSuccessToast(message) {
        const $toast = $("#toastPedidoSucesso");
        if ($toast.length) {
            $toast.find(".toast-body").text(message);
            bootstrap.Toast.getOrCreateInstance($toast[0], { delay: 3500 }).show();
        } else {
            alert(message);
        }
    }

    function formatBRL(value) {
        try {
            return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(Number(value ?? 0));
        } catch {
            const n = Number(value ?? 0);
            return "R$ " + n.toFixed(2).replace(".", ",");
        }
    }

    async function copyText(text, msgEl) {
        try {
            await navigator.clipboard.writeText(text);
        } catch {
            const ta = document.createElement("textarea");
            ta.value = text;
            document.body.appendChild(ta);
            ta.select();
            document.execCommand("copy");
            document.body.removeChild(ta);
        }
        if (msgEl) {
            msgEl.classList.remove("d-none");
            setTimeout(() => msgEl.classList.add("d-none"), 1800);
        }
    }

    // -----------------------------
    // PIX Timer (ExpiraEmUtc do backend)
    // -----------------------------
    let pixTimer = null;

    function stopPixTimer() {
        if (pixTimer) clearInterval(pixTimer);
        pixTimer = null;
    }

    function startPixTimer(expiraEmUtcIso) {
        stopPixTimer();

        const end = new Date(expiraEmUtcIso);
        if (isNaN(end.getTime())) {
            $("#pixExpiraEm").text("--:--:--");
            return;
        }

        pixTimer = setInterval(() => {
            const now = new Date();
            const diffMs = end.getTime() - now.getTime();
            const diffSec = Math.floor(diffMs / 1000);

            if (diffSec <= 0) {
                stopPixTimer();
                $("#pixExpiraEm").text("00:00:00");
                $("#pixStatusBox").removeClass("d-none").html(`
                <div class="alert alert-danger border-0 rounded-4 mb-0">
                    <i class="fa-solid fa-triangle-exclamation me-1"></i>
                    PIX expirado. Gere um novo código.
                </div>
            `);
                return;
            }

            // Cálculo para 24 horas (Horas, Minutos e Segundos)
            const h = String(Math.floor(diffSec / 3600)).padStart(2, "0");
            const m = String(Math.floor((diffSec % 3600) / 60)).padStart(2, "0");
            const s = String(diffSec % 60).padStart(2, "0");

            $("#pixExpiraEm").text(`${h}:${m}:${s}`);
        }, 1000);
    }

    // -----------------------------
    // Wizard UI
    // -----------------------------
    function updateWizardProgress(currentStep) {
        qsa(`#${BODY_ID} .wizard-step-indicator`).forEach(indicator => {
            const s = Number(indicator.dataset.step);
            indicator.classList.remove("active", "completed");
            if (s < currentStep) indicator.classList.add("completed");
            if (s === currentStep) indicator.classList.add("active");
        });
    }

    function setTitle(currentStep) {
        const titleEl = qs("#wizardTitle");
        if (titleEl) titleEl.textContent = titles[currentStep] || "";
    }

    function canGoNextStep1() {
        const horario = (document.querySelector("#inputHorarioFinal")?.value || "").trim();
        return !!horario;
    }

    function setButtons(currentStep) {
        const prevBtn = qs("#btnWizardPrev");
        const nextBtn = qs("#btnWizardNext");
        if (!nextBtn) return;

        nextBtn.removeAttribute("data-action");

        if (currentStep === 1) {
            if (prevBtn) prevBtn.classList.add("d-none");
            nextBtn.classList.remove("d-none");
            nextBtn.textContent = "Avançar";
            nextBtn.disabled = !canGoNextStep1();
            return;
        }

        if (currentStep === 2) {
            if (prevBtn) { prevBtn.classList.remove("d-none"); prevBtn.disabled = false; }
            nextBtn.classList.remove("d-none");
            nextBtn.textContent = "Confirmar pedido";
            nextBtn.disabled = false;
            nextBtn.setAttribute("data-action", "confirmar");
            return;
        }

        if (currentStep === 3) {
            if (prevBtn) prevBtn.classList.add("d-none");
            nextBtn.classList.remove("d-none");
            nextBtn.textContent = "Fechar";
            nextBtn.disabled = false;
            return;
        }
    }

    function showStep(nextStep) {
        step = nextStep;

        const steps = root()?.querySelectorAll(`#${BODY_ID} .wizard-step`) ?? [];
        if (!steps.length) return;

        steps.forEach(s => s.classList.add("d-none"));
        const current = root()?.querySelector(`#${BODY_ID} .wizard-step[data-step="${step}"]`);
        if (current) current.classList.remove("d-none");

        setTitle(step);
        setButtons(step);
        updateWizardProgress(step);
    }

    function atualizarResumo() {
        const diaTexto = $('input[name="diaSelecionado"]:checked')
            .next("label")
            .text()
            .replace(/\s+/g, " ")
            .trim();

        const horaTexto = $('input[name="horarioFinal"]:checked').next("label").text().trim();
        const obs = ($("#obsPedido").val() || "").trim() || "Nenhuma observação";

        $("#resumoDataHora").text(horaTexto ? `${diaTexto} às ${horaTexto}` : "Horário não selecionado");
        $("#resumoObs").text(obs);
    }

    function wizardInit() {
        step = 1;
        showStep(1);
    }

    // -----------------------------
    // Eventos gerais do Wizard
    // -----------------------------
    document.addEventListener("click", function (e) {
        if (!root()) return;

        // Voltar
        if (e.target.closest(`#${MODAL_ID} #btnWizardPrev`)) {
            if (step > 1) showStep(step - 1);
            return;
        }

        // Próximo / principal
        if (e.target.closest(`#${MODAL_ID} #btnWizardNext`)) {
            if (step === 1) {
                if (!canGoNextStep1()) {
                    alert("Selecione um dia e um horário para retirada.");
                    return;
                }
                atualizarResumo();
                showStep(2);
                return;
            }

            // Step 2 é AJAX (capturado abaixo)
            if (step === 2) return;

            // Step 3 fecha
            if (step === 3) {
                const modal = bootstrap.Modal.getInstance(document.getElementById(MODAL_ID));
                modal?.hide();
                return;
            }
        }
    });

    document.addEventListener('hidden.bs.modal', function (e) {
        if (e.target?.id !== MODAL_ID) return;

        if (window._pedidoConfirmado === true) {
            window.location.reload();
        }
    });

    // Step 1: troca dia / horários (mantém sua lógica)
    window.scrollCal = function (amount) {
        document.getElementById("calendarioBlocos")?.scrollBy({ left: amount, behavior: "smooth" });
    };

    $(document).on("change", ".btn-check-dia", function () {
        const target = $(this).val();
        $(".grade-horarios").addClass("d-none");

        const $containerAlvo = $(`#${target}`);
        if ($containerAlvo.length > 0) {
            $containerAlvo.removeClass("d-none");
            $("#msg-selecione-data").addClass("d-none");
        } else {
            $("#msg-selecione-data").removeClass("d-none");
        }

        $('input[name="horarioFinal"]').prop("checked", false);
        $("#inputHorarioFinal").val("");
        setButtons(1);
    });

    $(document).on("change", 'input[name="horarioFinal"]', function () {
        $("#inputHorarioFinal").val($(this).val());
        setButtons(1);
    });

    // -----------------------------
    // Step 2: Confirmar pedido (AJAX)
    // -----------------------------
    $(document).on("click", "#btnWizardNext[data-action=\"confirmar\"]", function (e) {
        e.preventDefault();
        e.stopImmediatePropagation();

        addLoading?.("Processando...");

        const horario = ($("#inputHorarioFinal").val() || "").trim();
        const observacao = ($("#obsPedido").val() || "").trim();

        if (!horario) {
            alert("Por favor, selecione um horário para retirada.");
            removeLoading?.();
            return;
        }

        const payload = { horarioRetirada: horario, observacao: observacao };

        const $btn = $(this);
        $btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm"></span> Processando...');

        $.ajax({
            url: "/Pedido/Confirmar",
            method: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(payload),
            success: function (res) {
                // Vai para step 3
                stopPixTimer();
                showStep(3);
                window._pedidoConfirmado = true;

                // Preenche Step 3 (VM/Response)
                $("#pixCopiaCola").val(res.pixCopiaCola || "");
                $("#pixChave").val(res.pixChave || "");
                $("#pixTxid").text(res.pedidoId || "-");
                $("#pixValorFinal").text(formatBRL(res.valor));
                $("#pixValorSinal").text(formatBRL(res.valor));

                if (res.qrCodeBase64) {
                    $("#pixQrImg").attr("src", "data:image/png;base64," + res.qrCodeBase64);
                } else {
                    $("#pixQrImg").attr("src", "");
                }

                if (res.expiraEmUtc) startPixTimer(res.expiraEmUtc);

                showSuccessToast("Pedido confirmado com sucesso! Agora finalize o pagamento via PIX.");

                // transforma o botão em "Fechar"
                $btn.prop("disabled", false).text("Fechar").removeAttr("data-action");

                removeLoading?.();
            },
            error: function (xhr) {
                $btn.prop("disabled", false).text("Confirmar pedido");
                alert(xhr.responseText || "Erro ao processar pedido.");
                removeLoading?.();
            }
        });
    });

    // -----------------------------
    // Step 3: ações (copiar/ja paguei)
    // -----------------------------
    $(document).on("click", "#btnCopiarPix", function () {
        copyText($("#pixCopiaCola").val(), document.getElementById("pixCopyMsg"));
    });

    $(document).on("click", "#btnCopiarChave", function () {
        copyText($("#pixChave").val(), document.getElementById("pixCopyMsg"));
    });

    $(document).on("click", "#btnJaPaguei", function () {
        $("#pixStatusBox").removeClass("d-none").html(`
      <div class="alert alert-warning border-0 rounded-4 mb-0">
        <i class="fa-regular fa-hourglass-half me-1"></i>
        Verificando pagamento... aguarde.
      </div>
    `);

        // DEMO: simula confirmação
        setTimeout(() => {
            $("#pixStatusBox").html(`
        <div class="alert alert-success border-0 rounded-4 mb-0">
          <i class="fa-solid fa-circle-check me-1"></i>
          Pagamento confirmado (DEMO). Seu pedido foi reservado!
        </div>
      `);
        }, 1500);
    });
})();
