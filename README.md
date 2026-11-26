<p align="center">
  <img src="frontend/public/brand/h2l-banner.jpg" alt="h2l — Human-to-Loop Workflow Integrator" width="720" />
</p>

# h2l — Human-to-Loop Workflow Integrator

**h2l (Human-in-the-Loop)** to uniwersalne narzędzie B2B (self-hosted / SaaS-ready) dla firm korzystających z silników workflow (**Camunda 8**, **n8n**, a także uniwersalnych webhooków / przyszłych integracji jak Zapier czy Make), które umożliwia szybkie włączenie człowieka w zautomatyzowany proces biznesowy.

- **Panel administratora i twórcy procesów** — własny kreator formularzy drag-and-drop z wersjonowaniem, logiką warunkową i podpięciem do etapów workflow.
- **Portal użytkownika końcowego (PWA)** — responsywna aplikacja mobilna/desktopowa z obsługą trybu offline, wielojęzycznością (**EN / DE / PL**) oraz pełną zgodnością z **WCAG 2.1 AA**.

---

## Zasoby marki (Logo i identyfikacja)

Wszystkie pliki brandingowe znajdują się w katalogu [`frontend/public/brand/`](frontend/public/brand/):

| Zasób | Plik | Zastosowanie |
| :--- | :--- | :--- |
| Pełny baner | [`h2l-banner.jpg`](frontend/public/brand/h2l-banner.jpg) | Dokumentacja, prezentacje, README |
| Samo logo (sygnet) | [`h2l-mark.svg`](frontend/public/brand/h2l-mark.svg) | Ikony, stopka, avatary, kompaktowe widoki |
| Sam napis (wordmark) | [`h2l-wordmark-light.svg`](frontend/public/brand/h2l-wordmark-light.svg) / [`h2l-wordmark-dark.svg`](frontend/public/brand/h2l-wordmark-dark.svg) | Nagłówki tekstowe, materiały marketingowe |
| Pełne logo (sygnet + napis) | [`h2l-logo-light.svg`](frontend/public/brand/h2l-logo-light.svg) / [`h2l-logo-dark.svg`](frontend/public/brand/h2l-logo-dark.svg) | Jasne i ciemne tła, nagłówki aplikacji |
| Ikony PWA i favicon | [`favicon.svg`](frontend/public/favicon.svg), [`icon-192x192.svg`](frontend/public/icons/icon-192x192.svg), [`icon-512x512.svg`](frontend/public/icons/icon-512x512.svg) | Karta przeglądarki, instalacja PWA |

W aplikacji Angular do renderowania logo służy komponent `BrandLogoComponent` (`frontend/src/app/shared/components/brand-logo/`) obsługujący warianty `full`, `mark` oraz `wordmark`.

---

## Struktura projektu

- [`backend/`](backend/) — API i logika domenowa (.NET / C# 10, Clean Architecture, CQRS, UnitOfWork, EF Core Code First + PostgreSQL, Quartz.NET)
- [`frontend/`](frontend/) — aplikacja kliencka Angular PWA (NgRx, Tailwind CSS, WCAG 2.1 AA, i18n EN/DE/PL, Cypress)
