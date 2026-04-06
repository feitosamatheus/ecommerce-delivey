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
        debugger
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

    // -----------------------------
    // Step 3: ações (copiar/ja paguei)
    // -----------------------------
    $(document).on("click", "#btnCopiarPix", function () {
        copyText($("#pixCopiaCola").val(), document.getElementById("pixCopyMsg"));
    });

    $(document).on("click", "#btnCopiarChave", function () {
        copyText($("#pixChave").val(), document.getElementById("pixCopyMsg"));
    });

    function atualizarVisibilidadePagamento() {
        const metodo = $('#selectPagamento').val(); 

        if (metodo === 'pix') {
            $('#step3-pix').removeClass('d-none');
            $('#step3-cartao').addClass('d-none');
        } else {
            $('#step3-pix').addClass('d-none');
            $('#step3-cartao').removeClass('d-none');
        }
    }

    const $msgPix = $('#msg-pagamento-pix');
    const $msgCartao = $('#msg-pagamento-cartao');

    $('#selectPagamento').on('change', function () {
        if ($(this).val() === 'pix') {
            $msgPix.removeClass('d-none');
            $msgCartao.addClass('d-none');
        } else {
            $msgPix.addClass('d-none');
            $msgCartao.removeClass('d-none');
        }
        atualizarVisibilidadePagamento();
    });

    $(document).on("click", "#btnJaPaguei", function () {
        debugger
        const $btn = $(this);
        const $statusBox = $("#pixStatusBox");
        const pedidoId = $("#pixPedidoId").val();

        $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span> Processando...');

        $statusBox.removeClass("d-none").html(`
            <div class="alert alert-warning border-0 rounded-4 mb-0">
                <i class="fa-regular fa-hourglass-half me-1 fa-spin"></i>
                Verificando pagamento... aguarde.
            </div>
        `);

        $.ajax({
            url: '/api/Pagamento/confirmar-pagamento',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ pedidoId: pedidoId }),
            success: function (response) {
                debugger
                if (response.success) {
                    $statusBox.html(`
                        <div class="alert alert-success border-0 rounded-4 mb-0 animate__animated animate__fadeIn">
                            <i class="fa-solid fa-circle-check me-1"></i>
                            Pagamento confirmado! Seu pedido foi reservado com sucesso.
                        </div>
                    `);

                    setTimeout(() => {
                        window.location.href = response.redirectUrl;
                    }, 2000);
                } else {
                    $statusBox.html(`
                        <div class="alert alert-danger border-0 rounded-4 mb-0">
                            <i class="fa-solid fa-circle-xmark me-1"></i>
                            ${response.message || "Pagamento não identificado. Tente novamente em instantes."}
                        </div>
                    `);
                    resetBtn($btn);
                }
            },
            error: function () {
                $statusBox.html(`
                    <div class="alert alert-danger border-0 rounded-4 mb-0">
                        <i class="fa-solid fa-triangle-exclamation me-1"></i>
                        Erro ao conectar com o servidor. Tente novamente.
                    </div>
                `);
                resetBtn($btn);
            }
        });
    });

    $(document).on("click", "#btnJaPagueiCartao", function () {
        const $btn = $(this);
        const $statusBox = $("#cartaoStatusBox");
        const pedidoId = $("#cartaoPedidoId").val() || $("#pixPedidoId").val();

        if (!pedidoId) {
            $statusBox.removeClass("d-none").html(`
            <div class="alert alert-danger border-0 rounded-4 mb-0">
                <i class="fa-solid fa-circle-xmark me-1"></i>
                Não foi possível identificar o pedido para consultar o pagamento.
            </div>
        `);
            return;
        }

        $btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-2"></span> Consultando...');

        $statusBox.removeClass("d-none").html(`
        <div class="alert alert-warning border-0 rounded-4 mb-0">
            <i class="fa-regular fa-hourglass-half me-1 fa-spin"></i>
            Verificando pagamento... aguarde.
        </div>
    `);

        $.ajax({
            url: "/api/Pagamento/confirmar-pagamento",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify({ pedidoId: pedidoId }),
            success: function (response) {
                if (response.success) {
                    $statusBox.html(`
                    <div class="alert alert-success border-0 rounded-4 mb-0 animate__animated animate__fadeIn">
                        <i class="fa-solid fa-circle-check me-1"></i>
                        ${response.message || "Pagamento confirmado com sucesso."}
                    </div>
                `);

                    $btn.prop("disabled", true).html('<i class="fa-solid fa-circle-check me-1"></i> Pagamento confirmado');

                    if (response.redirectUrl) {
                        setTimeout(() => {
                            window.location.href = response.redirectUrl;
                        }, 2000);
                    }
                } else {
                    $statusBox.html(`
                    <div class="alert alert-danger border-0 rounded-4 mb-0">
                        <i class="fa-solid fa-circle-xmark me-1"></i>
                        ${response.message || "Pagamento ainda não identificado. Tente novamente em instantes."}
                    </div>
                `);

                    $btn.prop("disabled", false).html('<i class="fa-solid fa-rotate-right me-1"></i> Já paguei / Atualizar status');
                }
            },
            error: function (xhr) {
                let mensagem = "Erro ao conectar com o servidor. Tente novamente.";

                if (xhr?.responseJSON?.message) {
                    mensagem = xhr.responseJSON.message;
                }

                $statusBox.html(`
                <div class="alert alert-danger border-0 rounded-4 mb-0">
                    <i class="fa-solid fa-triangle-exclamation me-1"></i>
                    ${mensagem}
                </div>
            `);

                $btn.prop("disabled", false).html('<i class="fa-solid fa-rotate-right me-1"></i> Já paguei / Atualizar status');
            }
        });
    });

    // Função auxiliar para resetar o botão caso o pagamento falhe
    function resetBtn($btn) {
        $btn.prop('disabled', false).html('<i class="fa-solid fa-circle-check me-1"></i> Já paguei via PIX');
    }

    async function pagarComCartao() {
        const pedidoId = $("#cartaoPedidoId").val();
        const paymentId = $("#cartaoPaymentId").val();

        const payload = {
            pedidoId: pedidoId,
            paymentId: paymentId,
            creditCard: {
                holderName: $("#ccHolderName").val().trim(),
                number: $("#ccNumber").val().trim(),
                expiryMonth: $("#ccExpiryMonth").val().trim(),
                expiryYear: $("#ccExpiryYear").val().trim(),
                ccv: $("#ccCvv").val().trim()
            },
            creditCardHolderInfo: {
                name: $("#holderName").val().trim(),
                email: $("#holderEmail").val().trim(),
                cpfCnpj: $("#holderCpfCnpj").val().trim(),
                postalCode: $("#holderPostalCode").val().trim(),
                addressNumber: $("#holderAddressNumber").val().trim(),
                addressComplement: $("#holderAddressComplement").val().trim(),
                phone: $("#holderPhone").val().trim(),
                mobilePhone: $("#holderMobilePhone").val().trim()
            }
        };

        const response = await fetch("/api/Pagamento/pagar-cartao", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        const res = await response.json();

        if (!response.ok || !res.sucesso) {
            throw new Error(res.mensagem || "Não foi possível processar o pagamento com cartão.");
        }

        return res;
    }

    $(document).on("click", "#btnPagarCartaoAgora", async function () {
        const $btn = $(this);
        const $statusBox = $("#cartaoStatusBox");

        $btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-2"></span> Processando...');

        $statusBox.removeClass("d-none").html(`
            <div class="alert alert-warning border-0 rounded-4 mb-0">
                <i class="fa-regular fa-hourglass-half me-1 fa-spin"></i>
                Processando pagamento no cartão...
            </div>
        `);

        try {
            const res = await pagarComCartao();

            $statusBox.html(`
                <div class="alert alert-success border-0 rounded-4 mb-0">
                    <i class="fa-solid fa-circle-check me-1"></i>
                    Pagamento processado com sucesso!
                </div>
            `);

            setTimeout(() => {
                if (res.redirectUrl) {
                    window.location.href = res.redirectUrl;
                }
            }, 1500);
        } catch (err) {
            $statusBox.html(`
                <div class="alert alert-danger border-0 rounded-4 mb-0">
                    <i class="fa-solid fa-circle-xmark me-1"></i>
                    ${err.message || "Erro ao processar pagamento."}
                </div>
            `);
            $btn.prop("disabled", false).html('<i class="fa-solid fa-lock me-2"></i> Pagar com Cartão Agora');
        }
    });

    const cardPatterns = {
        visa: /^4[0-9]{12}(?:[0-9]{3})?$/,
        mastercard: /^(5[1-5][0-9]{14}|2(22[1-9][0-9]{12}|2[3-9][0-9]{13}|[3-6][0-9]{14}|7[0-1][0-9]{13}|720[0-9]{12}))$/,
        amex: /^3[47][0-9]{13}$/,
        diners: /^3(?:0[0-5]|[68][0-9])[0-9]{11}$/,
        elo: /^((431274|438935|451416|457393|457631|457632|504175|627780|636297|636368|650031|650033|650035|650038|650039|650040|650041|650042|650043|650044|650045|650046|650047|650048|650049|650050|650051|650405|650406|650407|650408|650409|650410|650411|650412|650413|650414|650415|650416|650417|650418|650419|650420|650421|650422|650423|650424|650425|650426|650427|650428|650429|650430|650431|650432|650433|650434|650435|650436|650437|650438|650439|650485|650486|650487|650488|650530|650531|650532|650533|650534|650535|650536|650537|650538|650539|650541|650542|650543|650544|650545|650546|650547|650548|650549|650598|650700|650701|650702|650703|650704|650705|650706|650707|650708|650709|650710|650711|650712|650713|650714|650715|650720|650721|650722|650723|650724|650725|650726|650727|650901|650902|650903|650904|650905|650906|650907|650908|650909|650910|650911|650912|650913|650914|650915|650916|650917|650918|650919|650920|650976|650977|650978|651652|651653|651654|655000|655001)\d*)$/,
        hipercard: /^606282|^3841(0|4|6)0/,
        jcb: /^(?:2131|1800|35\d{3})\d{11}$/
    };

    function identificarBandeira(numero) {
        // Remove espaços e caracteres não numéricos
        const cleanNumber = numero.replace(/\D/g, '');

        for (let bandeira in cardPatterns) {
            if (cardPatterns[bandeira].test(cleanNumber)) {
                return bandeira;
            }
        }
        return 'unknown';
    }

    //-----------------------------------------------------------
    async function carregarCartaoPedido(pedidoId) {
        const tipoCobranca = Number($("#selectTipoCobranca").val() || 1);

        const response = await fetch("/api/Pagamento/criar-cobranca-cartao", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                pedidoId: pedidoId,
                tipoCobranca: tipoCobranca,
                description: "Pagamento do pedido"
            })
        });


        const res = await response.json();
        preencherResumoPagamentoPorCobranca(res);
        preencherStepCartao(res);
        await iniciarConexaoPagamento(res.payment.id);
        
        if (!response.ok || !res.sucesso) {
            throw new Error(res.mensagem || "Não foi possível gerar a cobrança por cartão.");
        }

        return res;
    }

    async function carregarPixPedido(pedidoId) {
        try {
            addLoading?.("Gerando PIX...");

            const tipoCobranca = Number($("#selectTipoCobranca").val() || 1);

            const response = await fetch("/api/Pagamento/criar-cliente-cobranca-pix", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    pedidoId: pedidoId,
                    tipoCobranca: tipoCobranca,
                    description: "Pagamento do pedido " + pedidoId
                })
            });

            const res = await response.json();

            if (!response.ok || !res.sucesso) {
                throw new Error(res.mensagem || "Não foi possível gerar o PIX.");
            }

            iniciarConexaoPagamento(res.payment.id);

            preencherStepPix(res);

            removeLoading?.();
            return res;
        } catch (error) {
            removeLoading?.();

            $("#pixStatusBox").removeClass("d-none").html(`
            <div class="alert alert-danger border-0 rounded-4 mb-0">
                <i class="fa-solid fa-triangle-exclamation me-1"></i>
                ${error.message || "Erro ao gerar cobrança PIX."}
            </div>
        `);

            throw error;
        }
    }

    function preencherStepPix(res) {
        preencherResumoPagamentoPorCobranca(res);

        const payment = res.payment || {};
        const pixQrCode = res.pixQrCode || {};

        $("#pixCopiaCola").val(pixQrCode.payload || "");
        $("#pixTxid").text(payment.id || "-");
        $("#pixValorFinal").text(formatBRL(payment.value || 0));

        if (pixQrCode.encodedImage) {
            $("#pixQrImg").attr("src", "data:image/png;base64," + pixQrCode.encodedImage);
        } else {
            $("#pixQrImg").attr("src", "");
        }

        if (pixQrCode.expirationDate) {
            startPixTimer(pixQrCode.expirationDate);
        } else {
            $("#pixExpiraEm").text("--:--:--");
        }

        $("#pixStatusBox").addClass("d-none").html("");

        if (res.pedidoPagamentoExistente) {
            uiNotify?.toast?.info?.("PIX já existente para este pedido. Reutilizando cobrança.");
        }
    }

    function preencherStepCartao(res) {
        const payment = res?.payment || {};
        const resumo = res?.resumoPedido || {};

        const tipoCobranca = (resumo.tipoCobranca || payment.tipoCobranca || "Sinal").toString();
        const valor = Number(payment.value || 0);
        const totalPedido = Number(resumo.totalPedido || 0);
        const valorRestanteRetirada = Number(resumo.valorRestanteRetirada || 0);

        const ehSinal = tipoCobranca.toLowerCase() === "sinal";

        $("#cartaoTipoCobranca").val(tipoCobranca);

        $("#cartaoDescricaoPagamento").text(
            ehSinal
                ? "Clique no botão abaixo para abrir nosso checkout seguro e realizar o pagamento do sinal com seu cartão."
                : "Clique no botão abaixo para abrir nosso checkout seguro e realizar o pagamento total com seu cartão."
        );

        $("#cartaoLabelCobranca").text(
            ehSinal ? "Sinal (Cartão)" : "Pagamento (Cartão)"
        );

        $("#cartaoValorCobranca").text(formatBRL(valor));
        $("#cartaoTotalPedido").text(formatBRL(totalPedido));
        $("#cartaoValorRestanteRetirada").text(formatBRL(valorRestanteRetirada));
    }

    let pagamentoConnection = null;

    async function iniciarConexaoPagamento(pedidoId) {
        console.log("[SignalR] iniciarConexaoPagamento chamado.", { pedidoId });

        if (!pedidoId) {
            console.warn("[SignalR] pedidoId não informado. Conexão não iniciada.");
            return;
        }

        if (pagamentoConnection) {
            console.log("[SignalR] Já existe uma conexão anterior. Encerrando conexão atual...");
            try {
                await pagamentoConnection.stop();
                console.log("[SignalR] Conexão anterior encerrada com sucesso.");
            } catch (error) {
                console.error("[SignalR] Erro ao encerrar conexão anterior:", error);
            }
        }

        console.log("[SignalR] Criando nova conexão com /hubs/pagamento ...");

        pagamentoConnection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/pagamento")
            .withAutomaticReconnect()
            .build();

        pagamentoConnection.onreconnecting((error) => {
            console.warn("[SignalR] Reconectando...", error);
        });

        pagamentoConnection.onreconnected((connectionId) => {
            console.log("[SignalR] Reconectado com sucesso.", { connectionId });
        });

        pagamentoConnection.onclose((error) => {
            if (error) {
                console.error("[SignalR] Conexão encerrada com erro:", error);
            } else {
                console.log("[SignalR] Conexão encerrada normalmente.");
            }
        });

        pagamentoConnection.on ("PagamentoConfirmado", function (data) {
            console.log("[SignalR] Evento recebido: PagamentoConfirmado", data);

            const mensagemSucesso = "Pagamento confirmado! Seu pedido foi reservado com sucesso.";

            $("#pixStatusBox").removeClass("d-none").html(`
                <div class="alert alert-success border-0 rounded-4 mb-0 animate__animated animate__fadeIn">
                    <i class="fa-solid fa-circle-check me-1"></i>
                    ${mensagemSucesso}
                </div>
            `);

                    $("#cartaoStatusBox").removeClass("d-none").html(`
                <div class="alert alert-success border-0 rounded-4 mb-0 animate__animated animate__fadeIn">
                    <i class="fa-solid fa-circle-check me-1"></i>
                    ${mensagemSucesso}
                </div>
            `);

            $("#btnJaPaguei").prop("disabled", true);
            $("#btnAbrirCheckoutCartao").prop("disabled", true);

            try {
                sessionStorage.setItem("pagamentoConfirmadoSucesso", "1");
                sessionStorage.setItem("pagamentoConfirmadoMensagem", mensagemSucesso);
            } catch (e) {
                console.warn("[SignalR] Não foi possível gravar mensagem no sessionStorage.", e);
            }

            setTimeout(() => {
                const loginUrl = "/Cliente/Login?pagamento=sucesso";
                console.log("[SignalR] Redirecionando para login.", { loginUrl });
                window.location.href = loginUrl;
            }, 1500);
        });

        pagamentoConnection.on("PagamentoPendente", function (data) {
            console.log("[SignalR] Evento recebido: PagamentoPendente", data);

            $("#pixStatusBox").removeClass("d-none").html(`
            <div class="alert alert-warning border-0 rounded-4 mb-0">
                <i class="fa-regular fa-hourglass-half me-1"></i>
                Status do pagamento: ${data.status}
            </div>
        `);
        });

        pagamentoConnection.on("PagamentoRecusado", function (data) {
            console.log("[SignalR] Evento recebido: PagamentoRecusado", data);

            $("#pixStatusBox").removeClass("d-none").html(`
                <div class="alert alert-danger border-0 rounded-4 mb-0">
                    <i class="fa-solid fa-circle-xmark me-1"></i>
                    O pagamento não foi confirmado. Status: ${data.status}
                </div>
            `);
        });

        try {
            console.log("[SignalR] Iniciando conexão...");
            await pagamentoConnection.start();
            console.log("[SignalR] Conexão iniciada com sucesso.");

            console.log("[SignalR] Entrando no grupo do pedido...", { pedidoId });
            await pagamentoConnection.invoke("EntrarNoGrupoPedido", pedidoId);
            console.log("[SignalR] Usuário conectado ao grupo do pedido com sucesso.", { pedidoId });
        } catch (error) {
            console.error("[SignalR] Erro ao iniciar conexão ou entrar no grupo:", error);
        }
    }

    $(document).on("click", "#btnAbrirCheckoutCartao", function () {
        const invoiceUrl = $(this).data("invoice-url");

        if (!invoiceUrl) {
            uiNotify.alert.error("A URL do checkout ainda não foi gerada.");
            return;
        }

        window.open(invoiceUrl, "_blank");
    });

    function preencherResumoPagamentoPorCobranca(res) {
        const payment = res?.payment || {};
        const resumo = res?.resumoPedido || {};

        const tipoCobranca = (resumo.tipoCobranca || payment.tipoCobranca || "Sinal").toString();
        const tituloPagamento = resumo.tituloPagamento || (tipoCobranca.toLowerCase() === "sinal"
            ? "Pagamento do Sinal (50%)"
            : "Pagamento");

        const valorPagarAgora = Number(payment.value || 0);
        const totalPedido = Number(resumo.totalPedido || 0);
        const valorRestanteRetirada = Number(resumo.valorRestanteRetirada || 0);

        $("#wizardTipoCobranca").val(tipoCobranca);
        $("#wizardTituloPagamento").text(tituloPagamento);
        $("#wizardValorPagarAgora").text(formatBRL(valorPagarAgora));
        $("#wizardValorRestanteRetirada").text(formatBRL(valorRestanteRetirada));
        $("#wizardTotalPedido").text(formatBRL(totalPedido));
    }

    //document.querySelector('.js-cal-prev').addEventListener('click', function () {
    //    window.scrollCal(-200); // Ajuste a quantidade conforme necessário
    //});

    //document.querySelector('.js-cal-next').addEventListener('click', function () {
    //    window.scrollCal(200); // Ajuste a quantidade conforme necessário
    //});

    $(document).on('click', '.js-cal-prev', function () {
        document.getElementById('calendarioBlocos').scrollBy({ left: -200, behavior: 'smooth' });
    });

    $(document).on('click', '.js-cal-next', function () {
        debugger
        document.getElementById('calendarioBlocos').scrollBy({ left: 200, behavior: 'smooth' });
    });

    function setStatusMensagemStep2(type, message, icon = "") {
        $("#statusMensagemStep2")
            .removeClass("d-none")
            .html(`
                <div class="alert alert-${type} border-0 rounded-4 mb-0 shadow-sm">
                    ${icon ? `<i class="${icon} me-2"></i>` : ""}
                    ${message}
                </div>
            `);
    }

    function limparStatusMensagemStep2() {
        $("#statusMensagemStep2").addClass("d-none").html("");
    }

    $(document).on("click", "#btnWizardNext[data-action=\"confirmar\"]", function (e) {
        if (step !== 2) return;
        e.preventDefault();
        e.stopImmediatePropagation();

        limparStatusMensagemStep2();
        addLoading?.("Processando...");

        const horario = ($("#inputHorarioFinal").val() || "").trim();
        const observacao = ($("#obsPedido").val() || "").trim();

        const tipoCobranca = Number($("#selectTipoCobranca").val() || 1);
        const tipoPagamento = ($("#selectPagamento").val() || "pix").toLowerCase();

        if (!horario) {
            setStatusMensagemStep2(
                "danger",
                "Por favor, selecione um horário para retirada.",
                "fa-solid fa-triangle-exclamation"
            );
            removeLoading?.();
            return;
        }

        const payload = {
            horarioRetirada: horario,
            observacao: observacao,
            tipoCobranca: tipoCobranca,
            tipoPagamento: tipoPagamento
        };

        const $btn = $(this);
        $btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm"></span> Processando...');

        $.ajax({
            url: "/Pedido/Confirmar",
            method: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(payload),
            success: async function (res) {
                stopPixTimer();
                showStep(3);
                atualizarVisibilidadePagamento();
                window._pedidoConfirmado = true;

                const pedidoId = res.pedidoId;
                const valor = res.valor;

                if (!pedidoId) {
                    setStatusMensagemStep2(
                        "danger",
                        "Pedido confirmado, mas o identificador do pedido não foi retornado.",
                        "fa-solid fa-circle-xmark"
                    );
                    $btn.prop("disabled", false).text("Confirmar pedido");
                    removeLoading?.();
                    return;
                }

                $("#pixPedidoId").val(pedidoId);
                $("#cartaoPedidoId").val(pedidoId);

                if (tipoPagamento === "pix") {
                    $("#pixCopiaCola").val("");
                    $("#pixTxid").text("Gerando...");
                    $("#pixQrImg").attr("src", "");
                    $("#pixValorFinal").text(formatBRL(valor || 0));

                    $("#pixStatusBox").removeClass("d-none").html(`
                        <div class="alert alert-warning border-0 rounded-4 mb-0">
                            <i class="fa-regular fa-hourglass-half me-1 fa-spin"></i>
                            Gerando cobrança PIX...
                        </div>
                    `);

                    try {
                        await carregarPixPedido(pedidoId);
                        await iniciarConexaoPagamento(pedidoId);

                        $btn.prop("disabled", false).text("Fechar").removeAttr("data-action");
                    } catch (err) {
                        setStatusMensagemStep2(
                            "danger",
                            err.message || "Erro ao gerar cobrança PIX.",
                            "fa-solid fa-circle-xmark"
                        );
                        $btn.prop("disabled", false).text("Fechar").removeAttr("data-action");
                    }

                    removeLoading?.();
                    return;
                }

                if (tipoPagamento === "cartao") {
                    $("#cartaoValorSinal").text(formatBRL(valor || 0));
                    $("#btnAbrirCheckoutCartao").data("invoice-url", "");
                    $("#cartaoStatusBox").removeClass("d-none").html(`
                        <div class="alert alert-warning border-0 rounded-4 mb-0">
                            <i class="fa-regular fa-credit-card me-1 fa-spin"></i>
                            Gerando cobrança no cartão...
                        </div>
                    `);

                    try {
                        const resCartao = await carregarCartaoPedido(pedidoId);

                        $("#cartaoStatusBox").html(`
                            <div class="alert alert-success border-0 rounded-4 mb-0">
                                <i class="fa-solid fa-circle-check me-1"></i>
                                Cobrança gerada com sucesso! Clique em <strong>"Abrir Checkout Seguro"</strong> para realizar o pagamento.
                            </div>
                        `);

                        if (!resCartao?.payment?.invoiceUrl) {
                            throw new Error("A URL de pagamento não foi retornada.");
                        }

                        $("#btnAbrirCheckoutCartao").data("invoice-url", resCartao.payment.invoiceUrl);
                        $btn.prop("disabled", false).text("Fechar").removeAttr("data-action");
                    } catch (err) {
                        setStatusMensagemStep2(
                            "danger",
                            err.message || "Erro ao gerar cobrança no cartão.",
                            "fa-solid fa-circle-xmark"
                        );
                        $btn.prop("disabled", false).text("Fechar").removeAttr("data-action");
                    }

                    removeLoading?.();
                    return;
                }

                setStatusMensagemStep2(
                    "danger",
                    "Forma de pagamento inválida.",
                    "fa-solid fa-triangle-exclamation"
                );

                $btn.prop("disabled", false).text("Confirmar pedido");
                removeLoading?.();
            },
            error: function (xhr) {
                setStatusMensagemStep2(
                    "danger",
                    xhr.responseText || "Erro ao processar pedido.",
                    "fa-solid fa-circle-xmark"
                );

                $btn.prop("disabled", false).text("Confirmar pedido");
                removeLoading?.();
            }
        });
    });
})();
