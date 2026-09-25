// Ponte entre o jogo (C# / WebAssembly) e o navegador: laço de quadros, tamanho do canvas, armazenamento,
// áudio e o SDK da Poki. Tudo aqui é defensivo — sem SDK (bloqueador de anúncios, teste local), sem
// localStorage (aba anônima, cota cheia) ou sem áudio, o jogo continua funcionando.
(function () {
    'use strict';

    // ---------- Áudio ----------
    // Os navegadores só liberam som depois de uma interação. Os contextos de áudio criados pelo jogo são anotados
    // (ver o trecho no index.html) e retomados no primeiro toque/tecla; ficam suspensos com a aba oculta e durante
    // anúncios.
    const audioContexts = window.megraceAudioContexts || [];
    let audioBlockedByAd = false;

    function setAudioRunning(running) {
        for (const context of audioContexts) {
            try {
                if (running && context.state !== 'running') {
                    context.resume();
                } else if (!running && context.state === 'running') {
                    context.suspend();
                }
            } catch (e) { /* contexto fechado: ignora */ }
        }
    }

    function audioShouldRun() {
        return !audioBlockedByAd && document.visibilityState !== 'hidden';
    }

    function unlockAudio() {
        if (audioShouldRun()) {
            setAudioRunning(true);
        }
    }

    for (const type of ['pointerdown', 'pointerup', 'touchend', 'keydown', 'mousedown']) {
        window.addEventListener(type, unlockAudio, { capture: true, passive: true });
    }

    document.addEventListener('visibilitychange', () => setAudioRunning(audioShouldRun()));

    // ---------- Página ----------
    // Setas, espaço e roda do mouse não podem rolar a página da Poki onde o jogo está embutido.
    window.addEventListener('keydown', (event) => {
        if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', ' ', 'Spacebar', 'Tab'].includes(event.key)) {
            event.preventDefault();
        }
    }, { passive: false });
    window.addEventListener('wheel', (event) => event.preventDefault(), { passive: false });
    document.addEventListener('contextmenu', (event) => event.preventDefault());

    // ---------- SDK da Poki ----------
    function sdk() {
        return window.PokiSDK && window.pokiReady ? window.PokiSDK : null;
    }

    function callSdk(name, ...args) {
        const poki = sdk();
        if (poki && typeof poki[name] === 'function') {
            try {
                return poki[name](...args);
            } catch (e) {
                console.warn('PokiSDK.' + name + ' falhou', e);
            }
        }
        return undefined;
    }

    let gameplayActive = false;
    let loadingDone = false;

    window.megrace = {
        startLoop(dotNetRef) {
            const canvas = document.getElementById('theCanvas');
            const tick = () => {
                try {
                    dotNetRef.invokeMethod('TickDotNet');
                } catch (e) {
                    window.megraceShowError(e);
                    return;
                }
                window.requestAnimationFrame(tick);
            };
            canvas.focus();
            window.requestAnimationFrame(tick);
        },

        // Tamanho em pixels reais do canvas (tamanho na página × densidade da tela), com teto pra não pesar em
        // telas enormes. Ajusta o próprio canvas quando muda; o jogo acompanha no mesmo quadro.
        canvasPixelSize() {
            const canvas = document.getElementById('theCanvas');
            const rect = canvas.getBoundingClientRect();
            let ratio = window.devicePixelRatio || 1;
            const maxPixels = 2560 * 1600;
            if (rect.width * rect.height * ratio * ratio > maxPixels) {
                ratio = Math.sqrt(maxPixels / Math.max(1, rect.width * rect.height));
            }
            const width = Math.max(1, Math.round(rect.width * ratio));
            const height = Math.max(1, Math.round(rect.height * ratio));
            if (canvas.width !== width || canvas.height !== height) {
                canvas.width = width;
                canvas.height = height;
            }
            return [width, height];
        },

        timezoneOffset() {
            return new Date().getTimezoneOffset();
        },

        storageGet(key) {
            try {
                return window.localStorage.getItem(key);
            } catch (e) {
                return null;
            }
        },

        storageSet(key, value) {
            try {
                window.localStorage.setItem(key, value);
                return true;
            } catch (e) {
                return false;
            }
        },

        loadingFinished() {
            if (loadingDone) {
                return;
            }
            loadingDone = true;
            window.megraceHideLoader();
            callSdk('gameLoadingFinished');
        },

        gameplayStart() {
            if (!gameplayActive) {
                gameplayActive = true;
                callSdk('gameplayStart');
            }
        },

        gameplayStop() {
            if (gameplayActive) {
                gameplayActive = false;
                callSdk('gameplayStop');
            }
        },

        // Intervalo comercial: a Poki decide se mostra um anúncio. O som só é suspenso se o anúncio começar de
        // fato; o jogo é avisado exatamente uma vez quando pode seguir (inclusive se o SDK não existir ou falhar).
        commercialBreak(dotNetRef) {
            let finished = false;
            const finish = () => {
                if (finished) {
                    return;
                }
                finished = true;
                audioBlockedByAd = false;
                setAudioRunning(audioShouldRun());
                try {
                    dotNetRef.invokeMethod('OnCommercialBreakFinished');
                } catch (e) {
                    console.warn(e);
                }
            };

            const result = callSdk('commercialBreak', () => {
                audioBlockedByAd = true;
                setAudioRunning(false);
            });
            if (result && typeof result.then === 'function') {
                result.then(finish, finish);
            } else {
                finish();
            }
        },
    };
})();
