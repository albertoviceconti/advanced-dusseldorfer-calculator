async function loadTesseract() {
    if (window.Tesseract) return window.Tesseract;
    await new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = 'https://unpkg.com/tesseract.js@5.0.3/dist/tesseract.min.js';
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
    return window.Tesseract;
}

window.runOcrBase64 = async (base64Data) => {
    const Tesseract = await loadTesseract();
    const buffer = Uint8Array.from(atob(base64Data), c => c.charCodeAt(0));
    const blob = new Blob([buffer], { type: 'image/png' }); // content type not critical for OCR
    const url = URL.createObjectURL(blob);
    try {
        const result = await Tesseract.recognize(url, 'deu');
        return result?.data?.text ?? '';
    } finally {
        URL.revokeObjectURL(url);
    }
};
