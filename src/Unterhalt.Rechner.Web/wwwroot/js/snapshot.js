window.downloadFile = (fileName, contentType, base64Data) => {
    const link = document.createElement('a');
    link.href = `data:${contentType};base64,${base64Data}`;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

window.saveFileWithPicker = async (fileName, contentType, base64Data) => {
    const blob = new Blob([Uint8Array.from(atob(base64Data), c => c.charCodeAt(0))], { type: contentType });

    if (window.showSaveFilePicker) {
        try {
            const handle = await window.showSaveFilePicker({
                suggestedName: fileName,
                types: [
                    {
                        description: 'JSON',
                        accept: { 'application/json': ['.json'] }
                    }
                ]
            });
            const writable = await handle.createWritable();
            await writable.write(blob);
            await writable.close();
            return true;
        } catch (err) {
            // User may have cancelled; fall back to download if not aborted.
            if (err && err.name === 'AbortError') {
                return false;
            }
        }
    }

    // Fallback: simple download (browser decides location)
    window.downloadFile(fileName, contentType, base64Data);
    return false;
};
