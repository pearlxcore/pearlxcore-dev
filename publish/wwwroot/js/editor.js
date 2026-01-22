// CRITICAL: Prevent browser from opening dropped files while still allowing editor handlers to run
(function() {
    const isEditorTarget = (target) => {
        return target?.id === 'Content' || target?.closest?.('.markdown-editor');
    };

    // Use capture so we run before native navigation. Always preventDefault to block navigation.
    const preventNavigation = (e) => {
        const insideEditor = isEditorTarget(e.target);
        e.preventDefault();
        if (!insideEditor) {
            // Stop navigation outside editor
            e.stopPropagation();
            console.log('Global drop prevented outside editor');
        } else {
            // Let editor-specific handlers run
            console.log('Global drop prevention applied (inside editor)');
        }
    };

    window.addEventListener('dragover', preventNavigation, true);
    window.addEventListener('drop', preventNavigation, true);
})();

// GitHub-style Markdown Editor with Drag-Drop and Preview
class MarkdownEditor {
    constructor(options = {}) {
        this.contentSelector = options.contentSelector || '#Content';
        this.apiEndpoint = options.apiEndpoint || '/Admin/Posts/UploadImage';
        this.previewEndpoint = options.previewEndpoint || '/Admin/Posts/PreviewMarkdown';
        this.isUploading = false;

        this.init();
    }

    init() {
        const textarea = document.querySelector(this.contentSelector);
        if (!textarea) {
            console.error('Textarea not found with selector:', this.contentSelector);
            return;
        }

        console.log('Initializing MarkdownEditor');

        // Create editor wrapper
        const wrapper = document.createElement('div');
        wrapper.className = 'markdown-editor';
        textarea.parentNode.insertBefore(wrapper, textarea);

        // Create tabs
        const tabsHTML = `
            <div class="editor-tabs">
                <button class="tab-btn active" data-tab="write" type="button">✏️ Write</button>
                <button class="tab-btn" data-tab="preview" type="button">👁️ Preview</button>
            </div>
        `;
        wrapper.innerHTML = tabsHTML;

        // Create content area
        const contentDiv = document.createElement('div');
        contentDiv.className = 'editor-content';
        wrapper.appendChild(contentDiv);

        // Write tab content
        const writeTab = document.createElement('div');
        writeTab.className = 'tab-pane active';
        writeTab.id = 'write-tab';
        contentDiv.appendChild(writeTab);
        writeTab.appendChild(textarea);

        // Preview tab content
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

        // Setup tab switching
        this.setupTabs(wrapper);

        // Setup drag-drop on the write tab and textarea
        this.setupDragDrop(textarea, writeTab);

        // Guard against unintended form submit during uploads
        this.setupSubmitGuard();

        // Setup auto-preview
        this.setupAutoPreview();

        console.log('MarkdownEditor initialized successfully');
    }

    setupTabs(wrapper) {
        const tabs = wrapper.querySelectorAll('.tab-btn');
        tabs.forEach(tab => {
            tab.addEventListener('click', (e) => {
                e.preventDefault();
                const tabName = e.target.dataset.tab;
                console.log('Tab clicked:', tabName);

                // Update tab buttons
                tabs.forEach(t => t.classList.remove('active'));
                e.target.classList.add('active');

                // Update tab panes
                const panes = wrapper.querySelectorAll('.tab-pane');
                panes.forEach(pane => pane.classList.remove('active'));
                wrapper.querySelector(`#${tabName}-tab`).classList.add('active');

                // Update preview if switching to preview
                if (tabName === 'preview') {
                    this.updatePreview();
                }
            });
        });
    }

    setupDragDrop(textarea, writeTab) {
        const dragOverClass = 'drag-over';
        const self = this;

        console.log('Setting up drag-drop...');
        console.log('Textarea element:', textarea);
        console.log('WriteTab element:', writeTab);

        // CRITICAL: Prevent ALL drops on window to stop browser navigation
        window.addEventListener('dragover', (e) => {
            e.preventDefault(); // Must prevent default to allow drop
            console.log('Window dragover prevented');
        }, false);

        window.addEventListener('drop', (e) => {
            e.preventDefault(); // ALWAYS prevent - stop browser from opening file
            e.stopPropagation();
            console.log('Window drop prevented - stopping browser navigation');
        }, false);

        // Textarea-specific handlers
        textarea.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.stopPropagation();
            textarea.classList.add(dragOverClass);
            console.log('Textarea dragover');
        }, false);

        textarea.addEventListener('dragleave', (e) => {
            textarea.classList.remove(dragOverClass);
        }, false);

        textarea.addEventListener('drop', (e) => {
            console.log('Textarea drop detected!!!');
            e.preventDefault();
            e.stopPropagation();
            textarea.classList.remove(dragOverClass);

            const files = e.dataTransfer.files;
            console.log('Files dropped on textarea:', files.length);
            
            if (files.length > 0) {
                self.handleImageFiles(files);
            }
        }, false);

        // Also handle drops on the editor wrapper itself
        writeTab.addEventListener('drop', (e) => {
            console.log('WriteTab drop detected');
            e.preventDefault();
            e.stopPropagation();
            
            const files = e.dataTransfer.files;
            if (files.length > 0) {
                self.handleImageFiles(files);
            }
        }, false);

        writeTab.addEventListener('dragover', (e) => {
            e.preventDefault();
            console.log('WriteTab dragover');
        }, false);

        console.log('Drag-drop setup complete');
    }

    handleImageFiles(files) {
        Array.from(files).forEach(file => {
            console.log('Processing file:', file.name, file.type);
            if (file.type.startsWith('image/')) {
                this.uploadImage(file);
            }
        });
    }

    uploadImage(file) {
        this.isUploading = true;
        const fileName = file.name.replace(/\.[^/.]+$/, ''); // Remove extension
        const uploadingText = `![Uploading ${fileName}...]()\n`;
        
        console.log('Starting upload for:', file.name);
        this.insertAtCursor(uploadingText);

        const formData = new FormData();
        formData.append('imageFile', file);

        // Get CSRF token from the page
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        console.log('CSRF token found:', !!token);

        fetch(this.apiEndpoint, {
            method: 'POST',
            body: formData,
            headers: token ? { 'X-CSRF-Token': token } : {}
        })
        .then(res => {
            console.log('Upload response status:', res.status);
            if (!res.ok) {
                throw new Error(`HTTP error! status: ${res.status}`);
            }
            return res.json();
        })
        .then(data => {
            console.log('Upload response:', data);
            console.log('Image URL from response:', data.imageUrl);
            
            if (data.success && data.imageUrl) {
                // Replace uploading text with actual markdown
                const newText = `![${fileName}](${data.imageUrl})\n`;
                console.log('Replacing "' + uploadingText.trim() + '" with "' + newText.trim() + '"');
                
                const content = this.textarea.value.replace(uploadingText, newText);
                this.textarea.value = content;
                console.log('Textarea updated, new length:', this.textarea.value.length);

                // Trigger input event to update preview
                this.textarea.dispatchEvent(new Event('input', { bubbles: true }));
                console.log('Image uploaded successfully:', data.imageUrl);
            } else {
                alert('Image upload failed: ' + (data.message || 'Unknown error'));
                console.error('Upload failed:', data);
                // Remove uploading text
                const content = this.textarea.value.replace(uploadingText, '');
                this.textarea.value = content;
            }
        })
        .catch(err => {
            console.error('Upload error:', err);
            alert('Image upload failed: ' + err.message);
            const content = this.textarea.value.replace(uploadingText, '');
            this.textarea.value = content;
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
            // Debounce preview update
            previewTimeout = setTimeout(() => {
                // Only update if preview tab is active
                if (this.wrapper.querySelector('#preview-tab').classList.contains('active')) {
                    this.updatePreview();
                }
            }, 500);
        });
    }

    setupSubmitGuard() {
        if (!this.form) {
            return;
        }

        this.form.addEventListener('submit', (e) => {
            if (this.isUploading) {
                console.warn('Blocked form submit during image upload');
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
            body: JSON.stringify({ markdown: markdown })
        })
        .then(res => res.json())
        .then(data => {
            if (data.html) {
                this.previewContent.innerHTML = data.html;
            }
        })
        .catch(err => {
            console.error('Preview error:', err);
        });
    }
}

// Initialize editor when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM loaded, initializing editor...');
    const editor = new MarkdownEditor({
        contentSelector: '#Content',
        apiEndpoint: '/Admin/Posts/UploadImage',
        previewEndpoint: '/Admin/Posts/PreviewMarkdown'
    });
});
