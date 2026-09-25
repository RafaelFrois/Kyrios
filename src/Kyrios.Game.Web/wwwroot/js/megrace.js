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
            if (context.state === 'closed') {
                continue;
            }
            try {
                const change = running
                    ? (context.state !== 'running' ? context.resume() : null)
                    : (context.state === 'running' ? context.suspend() : null);
                if (change && typeof change.catch === 'function') {
                    change.catch(() => { });
                }
            } catch (e) { /* navegador sem suporte: ignora */ }
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

    // ---------- Logo ----------
    // Decodificada pelo navegador enquanto o runtime .NET ainda carrega: textura com lados em potência de dois,
    // alfa pré-multiplicado e todos os níveis de mipmap prontos (o jogo só copia pra GPU).
    let logoInfo = null;
    let logoMips = null;

    function nextPowerOfTwo(value) {
        let result = 1;
        while (result < value) {
            result *= 2;
        }
        return result;
    }

    async function decodeImage(url) {
        const blob = await (await fetch(url)).blob();
        let source;
        if (typeof createImageBitmap === 'function') {
            source = await createImageBitmap(blob);
        } else {
            source = await new Promise((resolve, reject) => {
                const image = new Image();
                image.onload = () => resolve(image);
                image.onerror = reject;
                image.src = URL.createObjectURL(blob);
            });
        }
        const canvas = document.createElement('canvas');
        canvas.width = source.width;
        canvas.height = source.height;
        const context = canvas.getContext('2d', { willReadFrequently: true });
        context.drawImage(source, 0, 0);
        return { width: source.width, height: source.height, rgba: context.getImageData(0, 0, source.width, source.height).data };
    }

    const logoReady = (async () => {
        try {
            const { width, height, rgba } = await decodeImage('logo.png');
            let w = nextPowerOfTwo(width);
            let h = nextPowerOfTwo(height);
            const textureWidth = w;
            const textureHeight = h;
            let level = new Uint32Array(w * h);
            for (let y = 0; y < height; y++) {
                for (let x = 0; x < width; x++) {
                    const i = (y * width + x) * 4;
                    const a = rgba[i + 3];
                    const r = Math.round(rgba[i] * a / 255);
                    const g = Math.round(rgba[i + 1] * a / 255);
                    const b = Math.round(rgba[i + 2] * a / 255);
                    level[y * w + x] = (r | (g << 8) | (b << 16) | (a << 24)) >>> 0;
                }
            }
            const levels = [level];
            while (w > 1 || h > 1) {
                const hw = Math.max(1, w >> 1);
                const hh = Math.max(1, h >> 1);
                const stepX = w > 1 ? 1 : 0;
                const stepY = h > 1 ? w : 0;
                const next = new Uint32Array(hw * hh);
                for (let y = 0; y < hh; y++) {
                    for (let x = 0; x < hw; x++) {
                        const i = y * 2 * w + x * 2;
                        const p0 = level[i], p1 = level[i + stepX], p2 = level[i + stepY], p3 = level[i + stepY + stepX];
                        const rb = (p0 & 0x00FF00FF) + (p1 & 0x00FF00FF) + (p2 & 0x00FF00FF) + (p3 & 0x00FF00FF);
                        const ga = ((p0 >>> 8) & 0x00FF00FF) + ((p1 >>> 8) & 0x00FF00FF) + ((p2 >>> 8) & 0x00FF00FF) + ((p3 >>> 8) & 0x00FF00FF);
                        next[y * hw + x] = (((rb >>> 2) & 0x00FF00FF) | (((ga >>> 2) & 0x00FF00FF) << 8)) >>> 0;
                    }
                }
                levels.push(next);
                level = next;
                w = hw;
                h = hh;
            }
            const total = levels.reduce((sum, l) => sum + l.length, 0);
            const chain = new Uint32Array(total);
            let offset = 0;
            for (const l of levels) {
                chain.set(l, offset);
                offset += l.length;
            }
            logoMips = new Uint8Array(chain.buffer);
            logoInfo = [width, height, textureWidth, textureHeight];
        } catch (e) {
            console.warn('logo', e);
        }
    })();

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
            // Espera a logo ficar pronta (no máximo 3 s — sem ela o jogo segue do mesmo jeito).
            Promise.race([logoReady, new Promise((resolve) => setTimeout(resolve, 3000))])
                .then(() => window.requestAnimationFrame(tick));
        },

        logoInfo() {
            return logoInfo;
        },

        logoMips() {
            return logoMips;
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
            // Terceiro valor: pixels do canvas por pixel da página (×1000), pra converter mouse e toque.
            return [width, height, Math.round(1000 * width / Math.max(1, rect.width))];
        },

        // Celular/tablet (ponteiro "grosso" e sem mouse): o jogo já abre com os controles de toque.
        prefersTouch() {
            try {
                return window.matchMedia('(pointer: coarse)').matches && !window.matchMedia('(any-pointer: fine)').matches;
            } catch (e) {
                return 'ontouchstart' in window;
            }
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
