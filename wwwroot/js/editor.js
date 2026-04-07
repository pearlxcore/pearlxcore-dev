// Prevent browser navigation when dropping files on the page.
(function() {
    const isEditorTarget = (target) => target?.id === 'Content' || target?.closest?.('.markdown-editor');

    const preventNavigation = (e) => {
        const insideEditor = isEditorTarget(e.target);
        e.preventDefault();
        if (e.dataTransfer) {
            e.dataTransfer.dropEffect = 'copy';
        }
        if (!insideEditor) {
            e.stopPropagation();
        }
    };

    document.addEventListener('dragover', preventNavigation, true);
    document.addEventListener('drop', preventNavigation, true);
    window.addEventListener('dragover', preventNavigation, true);
    window.addEventListener('drop', preventNavigation, true);
})();

// GitHub-style Markdown Editor with drag-drop and preview.
class MarkdownEditor {
    constructor(options = {}) {
        this.contentSelector = options.contentSelector || '#Content';
        this.apiEndpoint = options.apiEndpoint || '/Admin/Posts/UploadImage';
        this.previewEndpoint = options.previewEndpoint || '/Admin/Posts/PreviewMarkdown';
        this.isUploading = false;
        this.filePicker = null;

        this.init();
    }

    init() {
        const textarea = document.querySelector(this.contentSelector);
        if (!textarea) {
            console.error('Textarea not found with selector:', this.contentSelector);
            return;
        }

        const wrapper = document.createElement('div');
        wrapper.className = 'markdown-editor';
        textarea.parentNode.insertBefore(wrapper, textarea);

        wrapper.innerHTML = `
            <div class="editor-tabs">
                <button class="tab-btn active" data-tab="write" type="button">Write</button>
                <button class="tab-btn" data-tab="preview" type="button">Preview</button>
            </div>
            <div class="editor-toolbar">
                <button class="toolbar-btn" data-action="bold" title="Bold (Ctrl+B)" type="button"><strong>B</strong></button>
                <button class="toolbar-btn" data-action="italic" title="Italic (Ctrl+I)" type="button"><em>I</em></button>
                <button class="toolbar-btn" data-action="heading1" title="Heading 1" type="button"># H1</button>
                <button class="toolbar-btn" data-action="heading2" title="Heading 2" type="button">## H2</button>
                <button class="toolbar-btn" data-action="link" title="Link" type="button">Link</button>
                <button class="toolbar-btn" data-action="codeblock" title="Code Block" type="button">Code Block</button>
                <button class="toolbar-btn" data-action="mermaid" title="Mermaid Diagram" type="button">Mermaid</button>
                <button class="toolbar-btn" data-action="uploadImage" title="Upload Image" type="button">Upload Image</button>
                <button class="toolbar-btn" data-action="quote" title="Quote" type="button">"</button>
                <button class="toolbar-btn" data-action="list" title="Bulleted List" type="button">- List</button>
                <button class="toolbar-btn" data-action="orderedList" title="Numbered List" type="button">1. List</button>
                <button class="toolbar-btn" data-action="center" title="Center Content" type="button">Center</button>
            </div>
        `;

        const contentDiv = document.createElement('div');
        contentDiv.className = 'editor-content';
        wrapper.appendChild(contentDiv);

        const writeTab = document.createElement('div');
        writeTab.className = 'tab-pane active';
        writeTab.id = 'write-tab';
        contentDiv.appendChild(writeTab);
        writeTab.appendChild(textarea);

        const previewTab = document.createElement('div');
        previewTab.className = 'tab-pane';
        previewTab.id = 'preview-tab';

        const previewContent = document.createElement('div');
        previewContent.className = 'preview-content article-body';
        previewTab.appendChild(previewContent);
        contentDiv.appendChild(previewTab);

        this.textarea = textarea;
        this.previewContent = previewContent;
        this.wrapper = wrapper;
        this.form = textarea.closest('form');

        this.setupTabs(wrapper);
        this.setupToolbar(wrapper, textarea);
        this.setupDragDrop(textarea, writeTab);
        this.setupSubmitGuard();
        this.setupAutoPreview();
        this.setupUploadButton(wrapper);
        this.setPreviewMode(false);

        if (textarea.value.trim()) {
            this.updatePreview();
        }
    }

    setupTabs(wrapper) {
        const tabs = wrapper.querySelectorAll('.tab-btn');
        tabs.forEach(tab => {
            tab.addEventListener('click', (e) => {
                e.preventDefault();
                const clickedTab = e.currentTarget;
                const tabName = clickedTab.dataset.tab;

                tabs.forEach(t => t.classList.remove('active'));
                clickedTab.classList.add('active');

                const panes = wrapper.querySelectorAll('.tab-pane');
                panes.forEach(pane => pane.classList.remove('active'));
                wrapper.querySelector(`#${tabName}-tab`).classList.add('active');
                this.setPreviewMode(tabName === 'preview');

                if (tabName === 'preview') {
                    this.updatePreview();
                }
            });
        });
    }

    setupToolbar(wrapper, textarea) {
        wrapper.querySelectorAll('.toolbar-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.preventDefault();
                this.insertMarkdown(textarea, e.currentTarget.dataset.action, wrapper);
            });
        });
    }

    setupUploadButton(wrapper) {
        const uploadButton = wrapper.querySelector('.toolbar-btn[data-action="uploadImage"]');
        if (!uploadButton) {
            return;
        }

        const picker = document.createElement('input');
        picker.type = 'file';
        picker.accept = 'image/*';
        picker.multiple = true;
        picker.className = 'editor-file-picker';
        picker.style.display = 'none';
        wrapper.appendChild(picker);
        this.filePicker = picker;

        picker.addEventListener('change', () => {
            if (picker.files && picker.files.length > 0) {
                this.handleImageFiles(picker.files);
            }
        });
    }

    setPreviewMode(isPreview) {
        this.wrapper.classList.toggle('is-previewing', isPreview);
    }

    insertMarkdown(textarea, action, wrapper = this.wrapper) {
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const selected = textarea.value.substring(start, end);

        switch (action) {
            case 'bold':
                return this.wrapSelection(textarea, start, end, '**', '**', 'text');
            case 'italic':
                return this.wrapSelection(textarea, start, end, '*', '*', 'text');
            case 'heading1':
                return this.prefixCurrentLines(textarea, start, end, '# ', 'Heading 1');
            case 'heading2':
                return this.prefixCurrentLines(textarea, start, end, '## ', 'Heading 2');
            case 'link':
                return this.wrapSelection(textarea, start, end, '[', '](url)', 'text');
            case 'codeblock':
                return this.insertCodeBlock(textarea, start, end, prompt('Code language (csharp, python, bash, or leave blank)', '') || '');
            case 'mermaid':
                return this.insertMermaidBlock(textarea, start, end);
            case 'quote':
                return this.prefixCurrentLines(textarea, start, end, '> ', 'Quote');
            case 'list':
                return this.formatList(textarea, start, end, '- ', 'List');
            case 'orderedList':
                return this.formatOrderedList(textarea, start, end);
            case 'center':
                return this.wrapCenteredBlock(textarea, start, end);
            case 'uploadImage':
                return this.triggerImageUpload(wrapper);
            default:
                return;
        }
    }

    wrapSelection(textarea, start, end, before, after, fallbackText) {
        const selected = textarea.value.substring(start, end) || fallbackText;
        const replacement = `${before}${selected}${after}`;
        const cursorStart = start + before.length;
        const cursorEnd = cursorStart + selected.length;
        this.replaceSelection(textarea, start, end, replacement, cursorStart, cursorEnd);
        this.updatePreview();
    }

    prefixCurrentLines(textarea, start, end, prefix, fallbackText) {
        const selected = textarea.value.substring(start, end);
        if (!selected) {
            const replacement = `${prefix}${fallbackText}`;
            this.replaceSelection(textarea, start, end, replacement, start + replacement.length, start + replacement.length);
            this.updatePreview();
            return;
        }

        const lines = selected.split('\n');
        const formatted = lines
            .map(line => line.trim().length ? `${prefix}${line.trimStart().replace(/^(?:\d+\.\s+|-+\s+|>\s+)/, '')}` : prefix.trimEnd())
            .join('\n');

        this.replaceSelection(textarea, start, end, formatted, start + formatted.length, start + formatted.length);
        this.updatePreview();
    }

    formatList(textarea, start, end, prefix, fallbackText) {
        const selected = textarea.value.substring(start, end);
        if (!selected) {
            const replacement = `${prefix}${fallbackText}`;
            this.replaceSelection(textarea, start, end, replacement, start + replacement.length, start + replacement.length);
            this.updatePreview();
            return;
        }

        const lines = selected.split('\n');
        const formatted = lines
            .map(line => line.trim().length ? `${prefix}${line.trimStart().replace(/^(?:\d+\.\s+|-+\s+|>\s+)/, '')}` : prefix.trimEnd())
            .join('\n');

        this.replaceSelection(textarea, start, end, formatted, start + formatted.length, start + formatted.length);
        this.updatePreview();
    }

    formatOrderedList(textarea, start, end) {
        const selected = textarea.value.substring(start, end);
        if (!selected) {
            const replacement = '1. Item';
            this.replaceSelection(textarea, start, end, replacement, start + replacement.length, start + replacement.length);
            this.updatePreview();
            return;
        }

        const lines = selected.split('\n');
        let count = 1;
        const formatted = lines
            .map(line => {
                const trimmed = line.trim();
                if (!trimmed) {
                    return '';
                }

                return `${count++}. ${trimmed.replace(/^(?:\d+\.\s+|-+\s+|>\s+)/, '')}`;
            })
            .join('\n');

        this.replaceSelection(textarea, start, end, formatted, start + formatted.length, start + formatted.length);
        this.updatePreview();
    }

    insertCodeBlock(textarea, start, end, language = '') {
        const selected = textarea.value.substring(start, end).trim() || 'code';
        const normalizedLanguage = (language || '').trim().toLowerCase();
        const openingFence = normalizedLanguage ? `\`\`\`${normalizedLanguage}\n` : '```\n';
        const replacement = `${openingFence}${selected}\n\`\`\``;
        const cursorStart = start + openingFence.length;
        const cursorEnd = cursorStart + selected.length;
        this.replaceSelection(textarea, start, end, replacement, cursorStart, cursorEnd);
        this.updatePreview();
    }

    insertMermaidBlock(textarea, start, end) {
        const selected = textarea.value.substring(start, end).trim();
        const template = selected || [
            'flowchart TD',
            '    A[Start App] --> B[Next Step]',
            '    B --> C[Finish]'
        ].join('\n');

        const replacement = `\`\`\`mermaid\n${template}\n\`\`\``;
        const cursorStart = start + '```mermaid\n'.length;
        const cursorEnd = cursorStart + template.length;
        this.replaceSelection(textarea, start, end, replacement, cursorStart, cursorEnd);
        this.updatePreview();
    }

    wrapCenteredBlock(textarea, start, end) {
        const selected = textarea.value.substring(start, end);
        const content = (selected || this.getCurrentLine(textarea, start)).trim() || 'Centered content';
        const prefix = '<div class="md-center">\n';
        const suffix = '\n</div>';
        const centeredContent = this.tryBuildCenteredImageMarkup(content) || content;
        const replacement = `${prefix}${centeredContent}${suffix}`;
        const cursorStart = start + prefix.length;
        const cursorEnd = cursorStart + centeredContent.length;
        this.replaceSelection(textarea, start, end, replacement, cursorStart, cursorEnd);
        this.updatePreview();
    }

    tryBuildCenteredImageMarkup(content) {
        const markdownImage = content.match(/^!\[([^\]]*)\]\(([^)\s]+)(?:\s+"([^"]*)")?\)$/);
        if (markdownImage) {
            const [, alt, src, title] = markdownImage;
            const titleAttr = title ? ` title="${this.escapeAttribute(title)}"` : '';
            return `<img src="${this.escapeAttribute(src)}" alt="${this.escapeAttribute(alt)}"${titleAttr} />`;
        }

        if (/^<img\b[^>]*>/i.test(content)) {
            return content;
        }

        return null;
    }

    escapeAttribute(value) {
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/"/g, '&quot;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    getCurrentLine(textarea, position) {
        const value = textarea.value;
        const lineStart = value.lastIndexOf('\n', Math.max(0, position - 1)) + 1;
        const lineEndIndex = value.indexOf('\n', position);
        const lineEnd = lineEndIndex === -1 ? value.length : lineEndIndex;
        return value.substring(lineStart, lineEnd);
    }

    triggerImageUpload(wrapper) {
        if (!this.filePicker) {
            const fallbackPicker = document.createElement('input');
            fallbackPicker.type = 'file';
            fallbackPicker.accept = 'image/*';
            fallbackPicker.multiple = true;
            fallbackPicker.style.display = 'none';
            wrapper.appendChild(fallbackPicker);
            this.filePicker = fallbackPicker;
            fallbackPicker.addEventListener('change', () => {
                if (fallbackPicker.files && fallbackPicker.files.length > 0) {
                    this.handleImageFiles(fallbackPicker.files);
                }
            });
        }

        this.filePicker.value = '';
        this.filePicker.click();
    }

    replaceSelection(textarea, start, end, replacement, selectionStart, selectionEnd) {
        const value = textarea.value;
        textarea.value = value.substring(0, start) + replacement + value.substring(end);
        textarea.focus();
        textarea.selectionStart = selectionStart;
        textarea.selectionEnd = selectionEnd;
    }

    setupDragDrop(textarea, writeTab) {
        const dragOverClass = 'drag-over';

        textarea.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.stopPropagation();
            e.dataTransfer.dropEffect = 'copy';
            textarea.classList.add(dragOverClass);
        }, false);

        textarea.addEventListener('dragleave', () => {
            textarea.classList.remove(dragOverClass);
        }, false);

        textarea.addEventListener('drop', (e) => {
            e.preventDefault();
            e.stopPropagation();
            textarea.classList.remove(dragOverClass);

            const files = e.dataTransfer.files;
            if (files.length > 0) {
                this.handleImageFiles(files);
            }
        }, false);

        writeTab.addEventListener('drop', (e) => {
            e.preventDefault();
            e.stopPropagation();

            const files = e.dataTransfer.files;
            if (files.length > 0) {
                this.handleImageFiles(files);
            }
        }, false);

        writeTab.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.stopPropagation();
            e.dataTransfer.dropEffect = 'copy';
        }, false);
    }

    handleImageFiles(files) {
        Array.from(files).forEach(file => {
            if (file.type.startsWith('image/')) {
                this.uploadImage(file);
            }
        });
    }

    uploadImage(file) {
        this.isUploading = true;
        const fileName = file.name.replace(/\.[^/.]+$/, '');
        const uploadingText = `![Uploading ${fileName}...]()\n`;

        this.insertAtCursor(uploadingText);

        const formData = new FormData();
        formData.append('imageFile', file);

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

        fetch(this.apiEndpoint, {
            method: 'POST',
            body: formData,
            headers: token ? { 'X-CSRF-Token': token } : {}
        })
        .then(res => {
            if (!res.ok) {
                throw new Error(`HTTP error! status: ${res.status}`);
            }
            return res.json();
        })
        .then(data => {
            if (data.success && data.imageUrl) {
                const newText = `![${fileName}](${data.imageUrl})\n`;
                const content = this.textarea.value.replace(uploadingText, newText);
                this.textarea.value = content;
                this.textarea.dispatchEvent(new Event('input', { bubbles: true }));
                if (window.pearlxMarkdownHighlight) {
                    window.pearlxMarkdownHighlight.highlightRoot(this.previewContent);
                }
            } else {
                alert('Image upload failed: ' + (data.message || 'Unknown error'));
                this.textarea.value = this.textarea.value.replace(uploadingText, '');
            }
        })
        .catch(err => {
            console.error('Upload error:', err);
            alert('Image upload failed: ' + err.message);
            this.textarea.value = this.textarea.value.replace(uploadingText, '');
        })
        .finally(() => {
            this.isUploading = false;
        });
    }

    insertAtCursor(text) {
        const textarea = this.textarea;
        const startPos = textarea.selectionStart;
        const endPos = textarea.selectionEnd;
        const before = textarea.value.substring(0, startPos);
        const after = textarea.value.substring(endPos);

        textarea.value = before + text + after;
        textarea.selectionStart = textarea.selectionEnd = startPos + text.length;
        textarea.focus();
    }

    setupAutoPreview() {
        let previewTimeout;
        this.textarea.addEventListener('input', () => {
            clearTimeout(previewTimeout);
            previewTimeout = setTimeout(() => {
                if (this.wrapper.querySelector('#preview-tab').classList.contains('active')) {
                    this.updatePreview();
                }
            }, 350);
        });
    }

    setupSubmitGuard() {
        if (!this.form) {
            return;
        }

        this.form.addEventListener('submit', (e) => {
            if (this.isUploading) {
                e.preventDefault();
                e.stopPropagation();
                return false;
            }
            return true;
        }, true);
    }

    updatePreview() {
        const markdown = this.textarea.value;

        fetch(this.previewEndpoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ markdown })
        })
        .then(res => res.json())
        .then(data => {
            if (data.html) {
                this.previewContent.innerHTML = data.html;
                if (window.pearlxMarkdownHighlight) {
                    window.pearlxMarkdownHighlight.highlightRoot(this.previewContent);
                }
            }
        })
        .catch(err => {
            console.error('Preview error:', err);
        });
    }
}

document.addEventListener('DOMContentLoaded', () => {
    if (document.querySelector('#Content')) {
        new MarkdownEditor({
            contentSelector: '#Content',
            apiEndpoint: '/Admin/Posts/UploadImage',
            previewEndpoint: '/Admin/Posts/PreviewMarkdown'
        });
    }
});
