# 🃏 CardMaker

**CardMaker** è una piattaforma professionale *data-driven* per la generazione, composizione, rendering e stampa di carte da gioco collezionabili (TCG).

Il progetto è architettato per supportare nativamente molteplici giochi di carte (TCG standard e giapponesi), con pipeline di rendering tipografico ad altissima precisione basata su **SkiaSharp** e conformità agli standard tipografici industriali di bleed, trim, safe zone e risoluzione (150 / 300 / 600 DPI).

---

## 🎮 Giochi Supportati Nativamente

| Gioco | Formato Fisico | Master Canvas (600 DPI) | Trim (Taglio) | Bleed | Safe Zone | Font Principali |
|---|---|---|---|---|---|---|
| **Yu-Gi-Oh! & Rush Duel** | Japanese (59 × 86 mm) | 1488 × 2126 px | 1394 × 2031 px | 2.0 mm (47 px) | 3.0 mm (71 px) | Matrix-Bold, Stone Serif, FOT-Rodin Pro M |
| **Pokémon TCG** | Standard Poker (63 × 88 mm) | 1583 × 2173 px | 1488 × 2079 px | 2.0 mm (47 px) | 3.0 mm (71 px) | Gill Sans Bold, Futura Bold |
| **Magic: The Gathering** | Standard Poker (63 × 88 mm) | 1583 × 2173 px | 1488 × 2079 px | 2.0 mm (47 px) | 3.0 mm (71 px) | Beleren Bold, MPlantin Regular & Italic |

---

## 📐 Specifiche Tecniche e Matematiche

### 1. Sistema di Coordinate Full-Bleed
Tutti i frame master coprono il canvas comprensivo dell'abbondanza tipografica (*bleed*):
- **Origine (0, 0)**: Bordo esterno dell'area di abbondanza (Bleed Box).
- **Linea di Taglio (Trim Box)**: Centrata all'interno dell'abbondanza a `+BleedPx` su tutti i lati.
- **Zona di Sicurezza (Safe Zone)**: Margine interno di rispetto per testi e simboli critici a `BleedPx + SafeZonePx`.

### 2. Formule di Conversione Millimetri / Pixel
$$\text{Pixel} = \left\lfloor \frac{\text{Millimetri} \times \text{DPI}}{25.4} + 0.5 \right\rfloor$$

- A 600 DPI: $1\text{ mm} \approx 23.622\text{ px}$
- A 300 DPI: $1\text{ mm} \approx 11.811\text{ px}$
- A 150 DPI: $1\text{ mm} \approx 5.906\text{ px}$

---

## 🖼️ Requisiti degli Asset Grafici

Tutti gli asset grafici originali sono di proprietà dei rispettivi autori. L'applicazione non distribuisce materiale protetto da copyright ed è dotata di generatori procedurali di frame e simboli segnaposto (ADR-010).

### Formato File
- **Immagini Frame e Simboli**: Formato PNG a 24 o 32 bit con canale Alpha trasparente (RGBA). Nessun profilo colore CMYK non standard incorporato.
- **Finestra Artwork**: I frame devono avere la finestra dedicata all'illustrazione con canale trasparente al 100% (Alpha = 0).
- **Font**: Formati TrueType (`.ttf`) e OpenType (`.otf`). I font web `.woff2` non sono supportati dal motore di rendering e vengono rifiutati.

---

## 🔣 Sintassi Inline dei Simboli

I campi di testo (come le descrizioni delle abilità, gli effetti e i costi di mana) supportano l'incorporamento dinamico dei glifi grafici tramite token:

```
{sym:<set-key>.<symbol-key>}
```

### Esempi Pratici
- **Yu-Gi-Oh!**: `{sym:attributes.dark}`, `{sym:spell-properties.quick-play}`, `{sym:stars.level}`
- **Pokémon**: `{sym:energies.fire}`, `{sym:energies.water}`, `{sym:energies.lightning}`
- **Magic: The Gathering**: `{sym:mana.tap}`, `{sym:mana.w}`, `{sym:mana.u}`, `{sym:mana.b}`, `{sym:mana.r}`, `{sym:mana.g}`

Il motore tipografico misura l'altezza ottica (*CapHeight*) del font corrente e centra verticalmente i glifi con offset geometrico pari a zero.

---

## 🚀 Avvio dell'Applicazione

### Prerequisiti
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Avvio Host Web
```bash
./run-web.sh
```
Il server Kestrel avvierà l'applicazione e rimarrà in ascolto su:
👉 **http://localhost:5240**

### Avvio Host Desktop (Blazor Hybrid + Photino)
```bash
./run-desktop.sh
```

---

## 🔑 Credenziali Amministratore Predefinite

All'avvio iniziale, il database SQLite viene popolato automaticamente con un account amministratore:
- **Email**: `admin@cardmaker.local`
- **Password**: `Admin123!456`

---

## 🧪 Esecuzione dei Test

Per eseguire l'intera suite di collaudo unitario e di rendering:
```bash
dotnet test tests/CardMaker.Application.Tests/CardMaker.Application.Tests.csproj
```
