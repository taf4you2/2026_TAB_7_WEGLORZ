import { spawnSync } from 'node:child_process';
import { mkdtempSync, readFileSync, rmSync, writeFileSync, mkdirSync, existsSync, statSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(scriptDirectory, '..', '..');
const helpPath = join(repositoryRoot, 'SystemAPI', 'wwwroot', 'help', 'admin-help.pl.json');
const outputDirectory = join(scriptDirectory, 'output');
const outputPath = resolve(process.argv[2] || join(outputDirectory, 'Instrukcja_panelu_administratora.pdf'));

const browserCandidates = [
    'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
    'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe',
    'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe'
];

const browserPath = browserCandidates.find(existsSync);
if (!browserPath) {
    throw new Error('Nie znaleziono Microsoft Edge ani Google Chrome.');
}

const help = JSON.parse(readFileSync(helpPath, 'utf8'));
const sectionOrder = ['global', 'dashboard', 'infra', 'tariffs', 'cards', 'sales', 'reports', 'staff', 'customers'];

function escapeHtml(value) {
    return String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;');
}

function sectionNumber(sectionId) {
    return sectionOrder.indexOf(sectionId) + 1;
}

function renderItems(sectionId) {
    const items = help.items
        .filter(item => item.section === sectionId)
        .sort((a, b) => (a.pdfOrder || 0) - (b.pdfOrder || 0));

    return items.map((item, index) => {
        const steps = item.steps?.length
            ? `<ol>${item.steps.map(step => `<li>${escapeHtml(step)}</li>`).join('')}</ol>`
            : '';
        const warnings = item.warnings?.length
            ? `<div class="warning"><strong>Uwaga</strong><ul>${item.warnings.map(warning => `<li>${escapeHtml(warning)}</li>`).join('')}</ul></div>`
            : '';

        return `
            <article class="instruction">
                <div class="instruction-number">${sectionNumber(sectionId)}.${index + 1}</div>
                <div class="instruction-body">
                    <h3>${escapeHtml(item.title)}</h3>
                    <div class="tooltip-copy"><strong>Szybka podpowiedź:</strong> ${escapeHtml(item.tooltip)}</div>
                    <p>${escapeHtml(item.description)}</p>
                    ${steps}
                    ${warnings}
                </div>
            </article>
        `;
    }).join('');
}

function renderSections() {
    return sectionOrder.map(sectionId => {
        const section = help.sections[sectionId];
        if (!section) return '';
        return `
            <section class="manual-section ${sectionId === 'global' ? 'page-break' : ''}" id="${escapeHtml(sectionId)}">
                <div class="section-kicker">Rozdział ${sectionNumber(sectionId)}</div>
                <h2>${escapeHtml(section.title)}</h2>
                <p class="section-lead">${escapeHtml(section.description)}</p>
                ${renderItems(sectionId)}
            </section>
        `;
    }).join('');
}

function renderToc() {
    return sectionOrder
        .filter(sectionId => help.sections[sectionId])
        .map(sectionId => `
            <li>
                <span>${sectionNumber(sectionId)}.</span>
                <strong>${escapeHtml(help.sections[sectionId].title)}</strong>
                <small>${help.items.filter(item => item.section === sectionId).length} opisów</small>
            </li>
        `).join('');
}

function renderQuickReference() {
    const importantIds = [
        'infra.add-lift',
        'infra.add-gate',
        'tariffs.add',
        'cards.actions',
        'reports.sales-range',
        'reports.throughput',
        'staff.add',
        'customers.list'
    ];
    const items = importantIds
        .map(id => help.items.find(item => item.id === id))
        .filter(Boolean);

    return items.map(item => `
        <div class="quick-card">
            <strong>${escapeHtml(item.title)}</strong>
            <span>${escapeHtml(item.tooltip)}</span>
        </div>
    `).join('');
}

const generatedDate = new Intl.DateTimeFormat('pl-PL', { dateStyle: 'long' }).format(new Date());
const html = `<!DOCTYPE html>
<html lang="pl">
<head>
    <meta charset="UTF-8">
    <title>${escapeHtml(help.title)}</title>
    <style>
        :root {
            --navy: #0f2942;
            --blue: #2563eb;
            --blue-soft: #eaf4ff;
            --ink: #172033;
            --muted: #526174;
            --line: #cbd5e1;
            --warning: #a96100;
        }

        * { box-sizing: border-box; }

        @page {
            size: A4;
            margin: 15mm 15mm 17mm;
        }

        body {
            margin: 0;
            color: var(--ink);
            background: #fff;
            font: 10.4pt/1.48 "Segoe UI", Arial, sans-serif;
        }

        h1, h2, h3 { color: var(--navy); page-break-after: avoid; }
        h1 { font-size: 33pt; line-height: 1.08; }
        h2 {
            margin: 5px 0 8px;
            padding-bottom: 6px;
            border-bottom: 2px solid #dbeafe;
            font-size: 21pt;
        }
        h3 { margin: 0 0 6px; font-size: 13pt; }
        p { margin: 7px 0; }
        ol, ul { margin: 8px 0 0 21px; padding: 0; }
        li { margin: 4px 0; }

        .cover {
            display: flex;
            flex-direction: column;
            justify-content: space-between;
            min-height: 257mm;
            padding: 22mm 14mm 14mm;
            color: white;
            background:
                radial-gradient(circle at 82% 12%, rgba(96, 165, 250, 0.62), transparent 28%),
                linear-gradient(145deg, #07192a, #143c61 62%, #2563eb);
            page-break-after: always;
        }
        .cover h1 { max-width: 160mm; color: white; }
        .cover .lead { max-width: 145mm; color: #dbeafe; font-size: 15pt; }
        .cover-badge {
            display: inline-block;
            align-self: flex-start;
            padding: 7px 12px;
            border: 1px solid rgba(255,255,255,.4);
            border-radius: 999px;
            background: rgba(255,255,255,.1);
            font-size: 9pt;
            font-weight: 700;
            letter-spacing: .08em;
            text-transform: uppercase;
        }
        .cover-meta {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 12px;
            padding-top: 18px;
            border-top: 1px solid rgba(255,255,255,.25);
            color: #dbeafe;
            font-size: 9.5pt;
        }

        .toc { page-break-after: always; }
        .toc-list {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 9px;
            margin: 16px 0 22px;
            padding: 0;
            list-style: none;
        }
        .toc-list li {
            display: grid;
            grid-template-columns: 28px 1fr;
            gap: 8px;
            padding: 11px;
            border: 1px solid var(--line);
            border-radius: 8px;
            background: #f8fafc;
            break-inside: avoid;
        }
        .toc-list li span { color: var(--blue); font-size: 15pt; font-weight: 800; }
        .toc-list li strong { display: block; color: var(--navy); }
        .toc-list li small { grid-column: 2; color: var(--muted); }

        .intro-box {
            padding: 14px;
            border-left: 4px solid var(--blue);
            background: var(--blue-soft);
            color: #1e3a5f;
        }

        .manual-section { margin-bottom: 24px; }
        .page-break { page-break-before: always; }
        .section-kicker {
            color: var(--blue);
            font-size: 9pt;
            font-weight: 800;
            letter-spacing: .08em;
            text-transform: uppercase;
        }
        .section-lead {
            margin-bottom: 15px;
            color: var(--muted);
            font-size: 11.5pt;
        }

        .instruction {
            display: grid;
            grid-template-columns: 35px 1fr;
            gap: 10px;
            margin: 0 0 13px;
            padding: 13px;
            border: 1px solid var(--line);
            border-radius: 9px;
            background: #fff;
            page-break-inside: avoid;
        }
        .instruction-number {
            display: grid;
            place-items: center;
            align-self: start;
            min-height: 35px;
            border-radius: 8px;
            color: white;
            background: var(--blue);
            font-size: 9pt;
            font-weight: 800;
        }
        .tooltip-copy {
            margin: 7px 0 9px;
            padding: 8px 10px;
            border-radius: 7px;
            color: #1e3a5f;
            background: var(--blue-soft);
            font-size: 9.5pt;
        }
        .warning {
            margin-top: 10px;
            padding: 9px 11px;
            border-left: 4px solid #f59e0b;
            background: #fff8e8;
            color: #6b3c00;
        }
        .warning strong { display: block; margin-bottom: 3px; }

        .editing {
            margin: 17px 0;
            padding: 15px;
            border: 1px solid #bfdbfe;
            border-radius: 9px;
            background: #eff6ff;
            page-break-inside: avoid;
        }
        .editing-grid {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            gap: 8px;
            margin-top: 10px;
        }
        .editing-grid div {
            padding: 9px;
            border-radius: 7px;
            background: white;
            border: 1px solid #dbeafe;
            text-align: center;
        }
        .editing-grid strong { display: block; color: var(--blue); font-size: 14pt; }

        .quick-reference {
            padding: 14px;
            border: 3px solid var(--navy);
            border-radius: 10px;
            page-break-before: always;
        }
        .quick-reference h2 { margin-top: 0; }
        .quick-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 9px;
        }
        .quick-card {
            padding: 10px;
            border: 1px solid var(--line);
            border-radius: 8px;
        }
        .quick-card strong { display: block; margin-bottom: 4px; color: var(--navy); }
        .quick-card span { color: var(--muted); }

        footer {
            margin-top: 20px;
            padding-top: 8px;
            border-top: 1px solid var(--line);
            color: var(--muted);
            font-size: 8pt;
        }
    </style>
</head>
<body>
    <section class="cover">
        <div>
            <span class="cover-badge">System sprzedaży biletów narciarskich</span>
            <h1>${escapeHtml(help.title)}</h1>
            <p class="lead">Kompletna instrukcja nawigacji, zarządzania infrastrukturą, taryfami, kartami, pracownikami oraz raportami.</p>
        </div>
        <div class="cover-meta">
            <div><strong>Wersja dokumentu</strong><br>${escapeHtml(help.version)}.0</div>
            <div><strong>Data wygenerowania</strong><br>${escapeHtml(generatedDate)}</div>
            <div><strong>Źródło treści</strong><br>admin-help.pl.json</div>
            <div><strong>Liczba opisanych elementów</strong><br>${help.items.length}</div>
        </div>
    </section>

    <section class="toc">
        <h1>Spis treści</h1>
        <p>${escapeHtml(help.intro)}</p>
        <ol class="toc-list">${renderToc()}</ol>
        <div class="intro-box">
            <strong>Jak korzystać z instrukcji w aplikacji?</strong>
            Włącz przełącznik „Podpowiedzi”, aby zobaczyć tooltipy. Przycisk „Pomoc” otwiera pełne opisy. Tryb edycji zapisuje zmiany lokalnie, a eksport JSON lub Markdown pozwala przenieść treść do kolejnej wersji dokumentacji.
        </div>
    </section>

    <section class="editing">
        <h2>Pomoc wbudowana w panel</h2>
        <p>Treść PDF pochodzi z tego samego pliku, który zasila tooltipy i panel instrukcji. Dzięki temu krótka podpowiedź, długi opis oraz dokument PDF pozostają spójne.</p>
        <div class="editing-grid">
            <div><strong>1</strong>Włącz podpowiedzi</div>
            <div><strong>2</strong>Otwórz Pomoc</div>
            <div><strong>3</strong>Edytuj tekst</div>
            <div><strong>4</strong>Eksportuj treść</div>
        </div>
    </section>

    ${renderSections()}

    <section class="quick-reference">
        <h2>Skrócona instrukcja administratora</h2>
        <div class="quick-grid">${renderQuickReference()}</div>
        <div class="warning">
            <strong>Bezpieczeństwo</strong>
            Przed dezaktywacją zasobu, zwrotem karty, zamknięciem zmiany lub nadaniem roli administratora sprawdź wybrany rekord i skutki operacji.
        </div>
    </section>

    <footer>
        Dokument wygenerowany automatycznie z pliku SystemAPI/wwwroot/help/admin-help.pl.json.
    </footer>
</body>
</html>`;

mkdirSync(outputDirectory, { recursive: true });
const temporaryDirectory = mkdtempSync(join(tmpdir(), 'tab-admin-manual-'));
const temporaryHtml = join(temporaryDirectory, 'instrukcja-panelu-administratora.html');

try {
    writeFileSync(temporaryHtml, html, 'utf8');

    const result = spawnSync(browserPath, [
        '--headless=new',
        '--disable-gpu',
        '--disable-extensions',
        '--no-pdf-header-footer',
        `--print-to-pdf=${outputPath}`,
        pathToFileURL(temporaryHtml).href
    ], { encoding: 'utf8' });

    if (result.status !== 0) {
        throw new Error(result.stderr || `Generowanie PDF zakończyło się kodem ${result.status}.`);
    }
    if (!existsSync(outputPath)) {
        throw new Error(`Nie utworzono pliku PDF: ${outputPath}`);
    }

    console.log(`Wygenerowano: ${outputPath}`);
    console.log(`Rozmiar: ${statSync(outputPath).size} bajtów`);
} finally {
    rmSync(temporaryDirectory, { recursive: true, force: true });
}
