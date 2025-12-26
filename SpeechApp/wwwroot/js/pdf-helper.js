// PDF.js helper for text extraction
window.pdfHelper = {
    // Initialize PDF.js
    async initialize() {
        if (window.pdfjsLib) {
            return true;
        }

        try {
            // PDF.js loaded from local files
            // Set worker source to local file
            if (window.pdfjsLib) {
                window.pdfjsLib.GlobalWorkerOptions.workerSrc = 'js/lib/pdf.worker.min.js';
                return true;
            }
            return false;
        } catch (error) {
            console.error('Failed to initialize PDF.js:', error);
            return false;
        }
    },

    // Extract text from PDF
    async extractText(base64Data, progressCallback) {
        try {
            // Initialize PDF.js
            await this.initialize();

            if (!window.pdfjsLib) {
                throw new Error('PDF.js library not loaded');
            }

            // Convert base64 to array buffer
            const binaryString = atob(base64Data);
            const bytes = new Uint8Array(binaryString.length);
            for (let i = 0; i < binaryString.length; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }

            // Load PDF document
            const loadingTask = window.pdfjsLib.getDocument({ data: bytes });
            const pdf = await loadingTask.promise;

            const totalPages = pdf.numPages;
            let fullText = '';
            let textContent = [];
            let imageCount = 0;
            let totalItems = 0;

            // Extract text from each page
            for (let pageNum = 1; pageNum <= totalPages; pageNum++) {
                const page = await pdf.getPage(pageNum);
                const content = await page.getTextContent();

                // Count items for scanned detection
                totalItems += content.items.length;

                // Get text from page
                const pageText = content.items
                    .map(item => item.str)
                    .join(' ');

                fullText += pageText + '\n\n';
                textContent.push({
                    page: pageNum,
                    text: pageText
                });

                // Check for images (scanned PDF detection)
                const ops = await page.getOperatorList();
                for (const fn of ops.fnArray) {
                    if (fn === window.pdfjsLib.OPS.paintImageXObject ||
                        fn === window.pdfjsLib.OPS.paintInlineImageXObject) {
                        imageCount++;
                    }
                }

                // Report progress
                if (progressCallback) {
                    const progress = Math.round((pageNum / totalPages) * 100);
                    progressCallback.invokeMethodAsync('Invoke', progress);
                }
            }

            // Detect if scanned (heuristic: more images than text items, or very little text)
            const isScanned = imageCount > totalItems ||
                             (fullText.trim().length < 100 && totalPages > 1);

            // Detect chapters (look for common chapter patterns)
            const chapters = this.detectChapters(fullText, textContent);

            return {
                success: true,
                text: fullText.trim(),
                pageCount: totalPages,
                isScanned: isScanned,
                chapters: chapters,
                metadata: {
                    pageCount: totalPages,
                    imageCount: imageCount,
                    textItemCount: totalItems
                }
            };

        } catch (error) {
            console.error('PDF extraction error:', error);
            return {
                success: false,
                errorMessage: error.message || 'Failed to extract text from PDF'
            };
        }
    },

    // Detect chapters in extracted text
    detectChapters(fullText, textContent) {
        const chapters = [];
        let chapterNumber = 0;

        // Common chapter patterns
        const chapterPatterns = [
            /^Chapter\s+(\d+|[IVX]+)[\s:.-]+(.+)$/im,
            /^(\d+)\.\s+(.+)$/m,
            /^CHAPTER\s+(\d+|[IVX]+)[\s:.-]+(.+)$/im,
            /^Part\s+(\d+|[IVX]+)[\s:.-]+(.+)$/im
        ];

        const lines = fullText.split('\n');
        let currentPosition = 0;

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i].trim();

            if (line.length < 3 || line.length > 100) {
                currentPosition += lines[i].length + 1;
                continue;
            }

            for (const pattern of chapterPatterns) {
                const match = line.match(pattern);
                if (match) {
                    // Close previous chapter
                    if (chapters.length > 0) {
                        chapters[chapters.length - 1].endPosition = currentPosition;
                    }

                    chapterNumber++;
                    const title = match[2] ? match[2].trim() : line;
                    const preview = lines.slice(i + 1, i + 4)
                        .filter(l => l.trim())
                        .join(' ')
                        .substring(0, 100);

                    chapters.push({
                        number: chapterNumber,
                        title: title,
                        startPosition: currentPosition,
                        endPosition: fullText.length,
                        preview: preview + (preview.length === 100 ? '...' : '')
                    });
                    break;
                }
            }

            currentPosition += lines[i].length + 1;
        }

        return chapters.length > 0 ? chapters : null;
    },

    // Get PDF metadata
    async getMetadata(base64Data) {
        try {
            await this.initialize();

            const binaryString = atob(base64Data);
            const bytes = new Uint8Array(binaryString.length);
            for (let i = 0; i < binaryString.length; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }

            const loadingTask = window.pdfjsLib.getDocument({ data: bytes });
            const pdf = await loadingTask.promise;
            const metadata = await pdf.getMetadata();

            return {
                title: metadata.info?.Title || null,
                author: metadata.info?.Author || null,
                subject: metadata.info?.Subject || null,
                creator: metadata.info?.Creator || null,
                producer: metadata.info?.Producer || null,
                creationDate: metadata.info?.CreationDate || null
            };
        } catch (error) {
            console.error('Failed to get PDF metadata:', error);
            return null;
        }
    }
};
