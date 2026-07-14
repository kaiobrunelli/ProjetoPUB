window.ApresentacaoJS = {
    inicializar: function () {

        // ── Animar contador de 0 → target ──────────────────────────────────
        const animarContador = (el) => {
            const target = parseFloat(el.dataset.target);
            if (isNaN(target)) return;
            const isMoney   = el.dataset.money   === 'true';
            const isPercent = el.dataset.percent === 'true';
            const duration  = 1800;
            const start     = performance.now();

            const fmt = (n) => {
                if (isMoney)   return 'R$ ' + Math.round(n).toLocaleString('pt-BR');
                if (isPercent) return n.toFixed(1).replace('.', ',') + '%';
                return Math.round(n).toString();
            };

            const tick = (now) => {
                const t    = Math.min((now - start) / duration, 1);
                const ease = 1 - Math.pow(1 - t, 3); // ease-out cúbico
                el.textContent = fmt(target * ease);
                if (t < 1) requestAnimationFrame(tick);
            };
            requestAnimationFrame(tick);
        };

        // ── Revelar elemento + animar contadores internos ──────────────────
        const revelar = (el) => {
            el.classList.add('visivel');
            el.querySelectorAll('[data-target]:not([data-animado])').forEach(c => {
                c.dataset.animado = 'true';
                animarContador(c);
            });
        };

        // ── IntersectionObserver para scroll reveal ────────────────────────
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    revelar(entry.target);
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.12 });

        document.querySelectorAll('.reveal').forEach(el => {
            const rect = el.getBoundingClientRect();
            if (rect.top < window.innerHeight * 0.95) {
                revelar(el); // já visível no load inicial
            } else {
                observer.observe(el);
            }
        });

        // ── Animar barras de progresso ──────────────────────────────────────
        const barObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const w = entry.target.dataset.width;
                    if (w) entry.target.style.width = w;
                    barObserver.unobserve(entry.target);
                }
            });
        }, { threshold: 0.2 });

        document.querySelectorAll('.barra-fill[data-width]').forEach(el => {
            const rect = el.getBoundingClientRect();
            if (rect.top < window.innerHeight * 0.95) {
                el.style.width = el.dataset.width;
            } else {
                barObserver.observe(el);
            }
        });
    }
};
