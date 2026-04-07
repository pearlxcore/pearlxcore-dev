(function() {
    const escapeHtml = (value) => {
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    };

    const wrap = (text, className) => `<span class="${className}">${text}</span>`;

    const initializeMermaid = () => {
        if (!window.mermaid || window.__pearlxMermaidInitialized) {
            return false;
        }

        window.mermaid.initialize({
            startOnLoad: false,
            theme: 'dark',
            securityLevel: 'strict'
        });
        window.__pearlxMermaidInitialized = true;
        return true;
    };

    const highlightCSharp = (input) => {
        let html = escapeHtml(input);
        html = html.replace(/(&quot;(?:\\.|[^&"])*&quot;|&#39;(?:\\.|[^&#'])*&#39;)/g, (match) => wrap(match, 'code-token code-token--string'));
        html = html.replace(/\/\/.*$/gm, (match) => wrap(match, 'code-token code-token--comment'));
        html = html.replace(/\b(\d+(\.\d+)?)\b/g, (match) => wrap(match, 'code-token code-token--number'));
        html = html.replace(/\b(public|private|protected|internal|static|readonly|const|class|record|struct|interface|enum|void|async|await|return|new|var|using|namespace|try|catch|finally|if|else|switch|case|for|foreach|while|do|break|continue|null|true|false|this|base|get|set|init|yield|typeof|is|as|nameof|params|ref|out|in)\b/g, (match) => wrap(match, 'code-token code-token--keyword'));
        html = html.replace(/\b(int|string|bool|double|decimal|float|long|short|byte|char|object|Task|DateTime|List|IEnumerable|Dictionary|Action|Func|IFormFile|HttpContext|Controller|ViewResult|JsonResult)\b/g, (match) => wrap(match, 'code-token code-token--type'));
        html = html.replace(/\b(Console|DateTime|String|Math|Path|File|Directory|Enumerable|Regex|JsonSerializer|Guid|Convert)\b/g, (match) => wrap(match, 'code-token code-token--builtin'));
        return html;
    };

    const highlightPython = (input) => {
        let html = escapeHtml(input);
        html = html.replace(/(&quot;(?:\\.|[^&"])*&quot;|&#39;(?:\\.|[^&'])*&#39;|&quot;&quot;&quot;[\s\S]*?&quot;&quot;&quot;|&#39;&#39;&#39;[\s\S]*?&#39;&#39;&#39;)/g, (match) => wrap(match, 'code-token code-token--string'));
        html = html.replace(/#.*$/gm, (match) => wrap(match, 'code-token code-token--comment'));
        html = html.replace(/\b(\d+(\.\d+)?)\b/g, (match) => wrap(match, 'code-token code-token--number'));
        html = html.replace(/\b(def|class|return|if|elif|else|for|while|break|continue|pass|import|from|as|try|except|finally|with|lambda|yield|in|is|not|and|or|True|False|None)\b/g, (match) => wrap(match, 'code-token code-token--keyword'));
        html = html.replace(/\b(print|len|range|list|dict|set|tuple|str|int|float|bool|open|enumerate|zip|map|filter|sorted|sum|min|max|input|type|isinstance|super)\b/g, (match) => wrap(match, 'code-token code-token--builtin'));
        return html;
    };

    const highlightBash = (input) => {
        let html = escapeHtml(input);
        html = html.replace(/(^|\s)(#.*)$/gm, (match, prefix, comment) => `${prefix}${wrap(comment, 'code-token code-token--comment')}`);
        html = html.replace(/(&quot;(?:\\.|[^&"])*&quot;|&#39;(?:\\.|[^&'])*&#39;)/g, (match) => wrap(match, 'code-token code-token--string'));
        html = html.replace(/\$[A-Za-z_][A-Za-z0-9_]*/g, (match) => wrap(match, 'code-token code-token--variable'));
        html = html.replace(/\B(-{1,2}[A-Za-z0-9_-]+)\b/g, (match) => wrap(match, 'code-token code-token--flag'));
        html = html.replace(/\b(sudo|bash|sh|zsh|fish|echo|cd|pwd|ls|cat|grep|find|mkdir|rm|cp|mv|chmod|chown|touch|curl|wget|git|dotnet|npm|pnpm|yarn|systemctl|service|docker|kubectl|ssh|scp|tar|unzip|zip|sed|awk|tail|head|less|more|ps|kill|export|source|apt|apt-get|brew|pip|python|python3|node)\b/g, (match) => wrap(match, 'code-token code-token--builtin'));
        return html;
    };

    const languageMap = {
        csharp: highlightCSharp,
        cs: highlightCSharp,
        csharp7: highlightCSharp,
        python: highlightPython,
        py: highlightPython,
        bash: highlightBash,
        sh: highlightBash,
        shell: highlightBash,
        zsh: highlightBash
    };

    const mermaidNodeLabelRegex = /(\b[A-Za-z_][A-Za-z0-9_]*)([\[\(\{])([^\[\]\(\)\{\}]*)?([\]\)\}])/g;

    const normalizeMermaidLine = (line) => {
        if (!line || !line.trim()) {
            return line;
        }

        return line.replace(mermaidNodeLabelRegex, (match, id, open, label, close) => {
            const trimmed = (label || '').trim();
            if (!trimmed) {
                return match;
            }

            if (trimmed.startsWith('"') && trimmed.endsWith('"')) {
                return match;
            }

            return `${id}${open}"${trimmed}"${close}`;
        });
    };

    const normalizeMermaidText = (input) => {
        if (!input) {
            return input;
        }

        return input
            .replace(/\r\n/g, '\n')
            .split('\n')
            .map(normalizeMermaidLine)
            .join('\n');
    };

    const ensureMermaidOverlay = () => {
        let overlay = document.getElementById('mermaidOverlay');
        if (overlay) {
            return overlay;
        }

        overlay = document.createElement('div');
        overlay.id = 'mermaidOverlay';
        overlay.className = 'mermaid-overlay';
        overlay.innerHTML = `
            <div class="mermaid-overlay__backdrop" data-mermaid-close></div>
            <div class="mermaid-overlay__dialog" role="dialog" aria-modal="true" aria-label="Mermaid diagram fullscreen view">
                <button type="button" class="mermaid-overlay__close" data-mermaid-close aria-label="Close diagram">X</button>
                <div class="mermaid-overlay__content"></div>
            </div>
        `;

        document.body.appendChild(overlay);

        overlay.querySelectorAll('[data-mermaid-close]').forEach((el) => {
            el.addEventListener('click', closeMermaidOverlay);
        });

        overlay.addEventListener('keydown', (event) => {
            if (event.key === 'Escape') {
                closeMermaidOverlay();
            }
        });

        return overlay;
    };

    const closeMermaidOverlay = () => {
        const overlay = document.getElementById('mermaidOverlay');
        if (!overlay) {
            return;
        }

        overlay.classList.remove('is-open');
        document.body.classList.remove('mermaid-overlay-open');
        const content = overlay.querySelector('.mermaid-overlay__content');
        if (content) {
            content.innerHTML = '';
        }
    };

    const openMermaidOverlay = (diagram) => {
        if (!diagram) {
            return;
        }

        const overlay = ensureMermaidOverlay();
        const content = overlay.querySelector('.mermaid-overlay__content');
        if (!content) {
            return;
        }

        const clone = diagram.cloneNode(true);
        clone.classList.add('mermaid-overlay__diagram');
        clone.removeAttribute('data-mermaid-fullscreen-bound');
        content.innerHTML = '';
        content.appendChild(clone);
        overlay.classList.add('is-open');
        document.body.classList.add('mermaid-overlay-open');

        requestAnimationFrame(() => {
            overlay.focus();
        });
    };

    const bindMermaidFullscreen = (diagram) => {
        if (!diagram || diagram.dataset.mermaidFullscreenBound === 'true') {
            return;
        }

        diagram.dataset.mermaidFullscreenBound = 'true';
        diagram.classList.add('mermaid--expandable');
        diagram.addEventListener('click', () => openMermaidOverlay(diagram));
        diagram.setAttribute('role', 'button');
        diagram.setAttribute('tabindex', '0');
        diagram.setAttribute('aria-label', 'Open Mermaid diagram fullscreen');
        diagram.addEventListener('keydown', (event) => {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                openMermaidOverlay(diagram);
            }
        });
    };

    const normalizeLanguage = (codeEl) => {
        const className = Array.from(codeEl.classList).find(cls => cls.startsWith('language-'));
        return className ? className.replace('language-', '').toLowerCase() : '';
    };

    const highlightCodeBlock = (codeEl) => {
        if (!codeEl || codeEl.dataset.highlighted === 'true') {
            return;
        }

        const language = normalizeLanguage(codeEl);
        const raw = codeEl.textContent || '';
        const highlighter = languageMap[language];

        if (!highlighter) {
            codeEl.innerHTML = escapeHtml(raw);
            codeEl.dataset.highlighted = 'true';
            return;
        }

        codeEl.innerHTML = highlighter(raw);
        codeEl.classList.add(`language-${language}`);
        codeEl.dataset.highlighted = 'true';
    };

    const highlightRoot = (root) => {
        if (!root) {
            return;
        }

        root.querySelectorAll('pre > code').forEach(highlightCodeBlock);
        if (window.mermaid) {
            initializeMermaid();
            renderMermaid(root);
        }
    };

    const renderMermaid = async (root) => {
        if (!root || !window.mermaid) {
            return;
        }

        const diagrams = root.querySelectorAll('.mermaid');
        if (!diagrams.length) {
            return;
        }

        diagrams.forEach((diagram) => {
            diagram.textContent = normalizeMermaidText(diagram.textContent || '');
        });

        try {
            if (typeof window.mermaid.run === 'function') {
                await window.mermaid.run({ nodes: Array.from(diagrams) });
            } else if (typeof window.mermaid.init === 'function') {
                window.mermaid.init(undefined, diagrams);
            }

            diagrams.forEach(bindMermaidFullscreen);
        } catch (error) {
            console.error('Mermaid render failed', error);
        }
    };

    const api = {
        highlightRoot,
        highlightCodeBlock
    };

    window.pearlxMarkdownHighlight = api;

    document.addEventListener('DOMContentLoaded', () => {
        highlightRoot(document.querySelector('.article-body'));
        highlightRoot(document.querySelector('.preview-content'));
        highlightRoot(document.querySelector('.project-detail__content'));
    });

    document.addEventListener('click', (event) => {
        if (event.target && event.target.matches('[data-mermaid-close]')) {
            closeMermaidOverlay();
        }
    });
})();


