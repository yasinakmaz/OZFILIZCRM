// Dosya Yolu: TeknikServisApp/wwwroot/js/app.js

/**
 * Teknik Servis Yönetim Sistemi - Ana JavaScript Dosyası
 * Global fonksiyonlar ve yardımcı metodlar
 */

// Global app objesi
window.TeknikServisApp = {

    /**
     * Uygulama başlatıldığında çalışır
     */
    init: function () {
        console.log('Teknik Servis App başlatıldı');
        this.setupGlobalEventHandlers();
        this.initializeTooltips();
        this.setupLoadingStates();
    },

    /**
     * Global event handler'ları kurar
     */
    setupGlobalEventHandlers: function () {
        // Blazor navigation event'ları
        document.addEventListener('DOMContentLoaded', function () {
            // Loading splash'ı gizle
            setTimeout(function () {
                const loadingSplash = document.querySelector('.loading-splash');
                if (loadingSplash) {
                    loadingSplash.style.opacity = '0';
                    loadingSplash.style.transition = 'opacity 0.5s ease';
                    setTimeout(() => loadingSplash.remove(), 500);
                }
            }, 1500);
        });

        // Form submit loading states
        document.addEventListener('submit', function (e) {
            const form = e.target;
            const submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn && !submitBtn.disabled) {
                TeknikServisApp.showButtonLoading(submitBtn);
            }
        });

        // Global error handler
        window.addEventListener('error', function (e) {
            console.error('Global error:', e.error);
            TeknikServisApp.handleGlobalError(e.error);
        });
    },

    /**
     * Bootstrap tooltip'lerini başlatır
     */
    initializeTooltips: function () {
        // Bootstrap 5 tooltip initialization
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    },

    /**
     * Loading state'lerini kurar
     */
    setupLoadingStates: function () {
        // Sayfa değişimlerinde loading göster
        let isNavigating = false;

        // Navigation başlangıcı
        document.addEventListener('beforeunload', function () {
            isNavigating = true;
            TeknikServisApp.showPageLoading();
        });

        // Navigation bitişi
        document.addEventListener('DOMContentLoaded', function () {
            if (isNavigating) {
                TeknikServisApp.hidePageLoading();
                isNavigating = false;
            }
        });
    },

    /**
     * Sayfa loading gösterir
     */
    showPageLoading: function () {
        const loadingHtml = `
            <div id="page-loading-overlay" style="
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(255, 255, 255, 0.9);
                display: flex;
                align-items: center;
                justify-content: center;
                z-index: 9999;
            ">
                <div class="text-center">
                    <div class="spinner-border text-primary" style="width: 3rem; height: 3rem;"></div>
                    <div class="mt-3">
                        <p class="text-muted">Yükleniyor...</p>
                    </div>
                </div>
            </div>
        `;
        document.body.insertAdjacentHTML('beforeend', loadingHtml);
    },

    /**
     * Sayfa loading'i gizler
     */
    hidePageLoading: function () {
        const overlay = document.getElementById('page-loading-overlay');
        if (overlay) {
            overlay.remove();
        }
    },

    /**
     * Button loading state gösterir
     */
    showButtonLoading: function (button) {
        if (!button) return;

        const originalText = button.innerHTML;
        button.setAttribute('data-original-text', originalText);
        button.disabled = true;
        button.innerHTML = `
            <span class="spinner-border spinner-border-sm me-2" role="status"></span>
            Yükleniyor...
        `;
    },

    /**
     * Button loading state'ini kaldırır
     */
    hideButtonLoading: function (button) {
        if (!button) return;

        const originalText = button.getAttribute('data-original-text');
        if (originalText) {
            button.innerHTML = originalText;
            button.disabled = false;
            button.removeAttribute('data-original-text');
        }
    },

    /**
     * Global hata yönetimi
     */
    handleGlobalError: function (error) {
        console.error('Uygulama hatası:', error);

        // Blazor error UI'ı göster
        const errorUI = document.getElementById('blazor-error-ui');
        if (errorUI) {
            errorUI.classList.add('show');
        }
    },

    /**
     * Başarı mesajı gösterir
     */
    showSuccessMessage: function (message, duration = 3000) {
        this.showToast(message, 'success', duration);
    },

    /**
     * Hata mesajı gösterir
     */
    showErrorMessage: function (message, duration = 5000) {
        this.showToast(message, 'danger', duration);
    },

    /**
     * Bilgi mesajı gösterir
     */
    showInfoMessage: function (message, duration = 3000) {
        this.showToast(message, 'info', duration);
    },

    /**
     * Toast mesajı gösterir
     */
    showToast: function (message, type = 'info', duration = 3000) {
        const toastHtml = `
            <div class="toast align-items-center text-white bg-${type} border-0" role="alert" style="
                position: fixed;
                top: 20px;
                right: 20px;
                z-index: 10000;
                min-width: 300px;
            ">
                <div class="d-flex">
                    <div class="toast-body">
                        ${message}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', toastHtml);

        const toastElement = document.body.lastElementChild;
        const toast = new bootstrap.Toast(toastElement, { delay: duration });
        toast.show();

        // Toast kapandıktan sonra DOM'dan kaldır
        toastElement.addEventListener('hidden.bs.toast', function () {
            toastElement.remove();
        });
    },

    /**
     * Onay dialog'u gösterir
     */
    confirm: function (message, title = 'Onay') {
        return new Promise((resolve) => {
            const result = window.confirm(message);
            resolve(result);
        });
    },

    /**
     * File input preview işlevi
     */
    previewImage: function (input, previewId) {
        if (input.files && input.files[0]) {
            const reader = new FileReader();

            reader.onload = function (e) {
                const preview = document.getElementById(previewId);
                if (preview) {
                    preview.src = e.target.result;
                    preview.style.display = 'block';
                }
            };

            reader.readAsDataURL(input.files[0]);
        }
    },

    /**
     * File'ı base64'e çevirir
     */
    fileToBase64: function (file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.readAsDataURL(file);
            reader.onload = () => {
                // Data URL'den base64 kısmını ayır
                const base64 = reader.result.split(',')[1];
                resolve(base64);
            };
            reader.onerror = error => reject(error);
        });
    },

    /**
     * Tarih formatları
     */
    formatDate: function (date, format = 'dd.MM.yyyy') {
        if (!date) return '';

        const d = new Date(date);
        const day = String(d.getDate()).padStart(2, '0');
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const year = d.getFullYear();
        const hours = String(d.getHours()).padStart(2, '0');
        const minutes = String(d.getMinutes()).padStart(2, '0');

        switch (format) {
            case 'dd.MM.yyyy':
                return `${day}.${month}.${year}`;
            case 'dd.MM.yyyy HH:mm':
                return `${day}.${month}.${year} ${hours}:${minutes}`;
            case 'yyyy-MM-dd':
                return `${year}-${month}-${day}`;
            default:
                return d.toLocaleDateString('tr-TR');
        }
    },

    /**
     * Para formatı
     */
    formatCurrency: function (amount) {
        if (amount === null || amount === undefined) return '-';
        return new Intl.NumberFormat('tr-TR', {
            style: 'currency',
            currency: 'TRY'
        }).format(amount);
    },

    /**
     * Telefon formatı
     */
    formatPhone: function (phone) {
        if (!phone) return '';
        const cleaned = phone.replace(/\D/g, '');

        if (cleaned.length === 10) {
            return cleaned.replace(/(\d{3})(\d{3})(\d{2})(\d{2})/, '($1) $2 $3 $4');
        } else if (cleaned.length === 11 && cleaned.startsWith('0')) {
            return cleaned.replace(/(\d{1})(\d{3})(\d{3})(\d{2})(\d{2})/, '$1 $2 $3 $4 $5');
        }

        return phone;
    },

    /**
     * Local storage yardımcıları
     */
    storage: {
        set: function (key, value) {
            try {
                localStorage.setItem(key, JSON.stringify(value));
            } catch (e) {
                console.warn('LocalStorage write error:', e);
            }
        },

        get: function (key) {
            try {
                const item = localStorage.getItem(key);
                return item ? JSON.parse(item) : null;
            } catch (e) {
                console.warn('LocalStorage read error:', e);
                return null;
            }
        },

        remove: function (key) {
            try {
                localStorage.removeItem(key);
            } catch (e) {
                console.warn('LocalStorage remove error:', e);
            }
        }
    },

    /**
     * Print fonksiyonu
     */
    print: function (elementId) {
        const element = document.getElementById(elementId);
        if (!element) {
            console.error('Print element not found:', elementId);
            return;
        }

        const printWindow = window.open('', '_blank');
        printWindow.document.write(`
            <!DOCTYPE html>
            <html>
            <head>
                <title>Yazdır</title>
                <link href="https://cdnjs.cloudflare.com/ajax/libs/bootstrap/5.3.0/css/bootstrap.min.css" rel="stylesheet" />
                <style>
                    @media print {
                        body { margin: 0; }
                        .no-print { display: none !important; }
                    }
                </style>
            </head>
            <body>
                ${element.innerHTML}
            </body>
            </html>
        `);

        printWindow.document.close();
        printWindow.print();
        printWindow.close();
    }
};

// Blazor interop fonksiyonları
window.blazorInterop = {
    showToast: function (message, type, duration) {
        TeknikServisApp.showToast(message, type, duration);
    },

    confirm: function (message) {
        return TeknikServisApp.confirm(message);
    },

    previewImage: function (input, previewId) {
        TeknikServisApp.previewImage(input, previewId);
    },

    print: function (elementId) {
        TeknikServisApp.print(elementId);
    }
};

// Uygulama başlatma
document.addEventListener('DOMContentLoaded', function () {
    TeknikServisApp.init();
});

// Export for modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = TeknikServisApp;
}