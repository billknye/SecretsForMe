export function downloadFileBytes(fileName, contentType, fileBytes) {
    // Create the URL
    const file = new File([fileBytes], fileName, { type: contentType });
    const exportUrl = URL.createObjectURL(file);
    // Create the <a> element and click on it
    const a = document.createElement("a");
    document.body.appendChild(a);
    a.href = exportUrl;
    a.download = fileName;
    a.target = "_self";
    a.click();
    // We don't need to keep the url, let's release the memory
    // On Safari it seems you need to comment this line... (please let me know if you know why)
    try {
        URL.revokeObjectURL(exportUrl);
    }
    catch {
        // Empty on purpose
    }
}
//# sourceMappingURL=fileUtilities.js.map