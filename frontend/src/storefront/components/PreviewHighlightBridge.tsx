import {
  useEffect,
} from "react";

type HighlightTarget =
  | "store.name"
  | "announcement"
  | "hero.eyebrow"
  | "hero.title"
  | "hero.description"
  | "hero.cta"
  | "hero.primaryImage"
  | "hero.secondaryImage"
  | "categories.eyebrow"
  | "categories.title"
  | "products.eyebrow"
  | "products.title"
  | "banner.eyebrow"
  | "banner.title"
  | "banner.body"
  | "banner.cta"
  | "banner.image"
  | "story.eyebrow"
  | "story.title"
  | "story.body"
  | "story.cta"
  | "story.image";

interface HighlightMessage {
  type?: string;

  highlight?: {
    target?: HighlightTarget;
    label?: string;
    value?: string;
  };
}

const HIGHLIGHT_CLASS =
  "ofoq-builder-preview-highlight";

function normalizeText(
  value: string,
) {
  return value
    .replace(
      /\s+/g,
      " ",
    )
    .trim();
}

function clearHighlight() {
  document
    .querySelectorAll(
      `.${HIGHLIGHT_CLASS}`,
    )
    .forEach(
      (element) => {
        element.classList.remove(
          HIGHLIGHT_CLASS,
        );
      },
    );
}

function findExactText(
  selector: string,
  value: string,
) {
  const wanted =
    normalizeText(
      value,
    );

  if (!wanted) {
    return null;
  }

  return (
    Array.from(
      document.querySelectorAll<HTMLElement>(
        selector,
      ),
    ).find(
      (element) =>
        normalizeText(
          element.textContent ??
            "",
        ) === wanted,
    ) ??
    null
  );
}

function findImage(
  value: string,
) {
  if (!value) {
    return null;
  }

  let absolute =
    value;

  try {
    absolute =
      new URL(
        value,
        window.location.href,
      ).href;
  }
  catch {
    absolute =
      value;
  }

  return (
    Array.from(
      document.images,
    ).find(
      (image) =>
        image.src ===
          absolute ||
        image.getAttribute(
          "src",
        ) === value,
    ) ??
    null
  );
}

function findTarget(
  target: HighlightTarget,
  value: string,
): HTMLElement | null {
  switch (target) {
    case "store.name":
      return findExactText(
        "a",
        value,
      );

    case "announcement":
      return findExactText(
        "div, span, p",
        value,
      );

    case "hero.eyebrow":
      return findExactText(
        "p, span",
        value,
      );

    case "hero.title":
      return findExactText(
        "h1",
        value,
      );

    case "hero.description":
      return findExactText(
        "p",
        value,
      );

    case "hero.cta":
      return findExactText(
        "a, button",
        value,
      );

    case "hero.primaryImage":
    case "hero.secondaryImage":
    case "banner.image":
    case "story.image":
      return findImage(
        value,
      );

    case "categories.eyebrow":
      return findExactText(
        "p, span",
        value,
      );

    case "categories.title":
      return findExactText(
        "h2",
        value,
      );

    case "products.eyebrow":
      return findExactText(
        "p, span",
        value,
      );

    case "products.title":
      return findExactText(
        "h2",
        value,
      );

    case "banner.eyebrow":
      return findExactText(
        "p, span",
        value,
      );

    case "banner.title":
      return findExactText(
        "h2",
        value,
      );

    case "banner.body":
      return findExactText(
        "p",
        value,
      );

    case "banner.cta":
      return findExactText(
        "a, button",
        value,
      );

    case "story.eyebrow":
      return findExactText(
        "p, span",
        value,
      );

    case "story.title":
      return findExactText(
        "h2",
        value,
      );

    case "story.body":
      return findExactText(
        "p",
        value,
      );

    case "story.cta":
      return findExactText(
        "a, button",
        value,
      );
  }
}

function highlightElement(
  element: HTMLElement,
) {
  clearHighlight();

  element.classList.add(
    HIGHLIGHT_CLASS,
  );

  element.scrollIntoView({
    behavior: "smooth",
    block: "center",
    inline: "nearest",
  });
}

export function PreviewHighlightBridge() {
  useEffect(() => {
    const params =
      new URLSearchParams(
        window.location.search,
      );

    if (
      params.get(
        "builderPreview",
      ) !== "1"
    ) {
      return;
    }

    function handleMessage(
      event: MessageEvent<HighlightMessage>,
    ) {
      if (
        event.origin !==
        window.location.origin
      ) {
        return;
      }

      if (
        event.data?.type !==
        "OFOQ_STOREFRONT_HIGHLIGHT"
      ) {
        return;
      }

      const highlight =
        event.data.highlight;

      if (
        !highlight?.target
      ) {
        return;
      }

      const element =
        findTarget(
          highlight.target,
          highlight.value ??
            "",
        );

      if (!element) {
        return;
      }

      highlightElement(
        element,
      );
    }

    // Selection is active only in the same-origin visual editor iframe.
    // It does not change behaviour on the public storefront.
    function handleBuilderSelection(event: MouseEvent) {
      const clicked = event.target;
      if (!(clicked instanceof Element) || window.parent === window) return;
      const explicit = clicked.closest<HTMLElement>("[data-rukn-target]");
      let target = explicit?.dataset.ruknTarget || null;
      if (!target) {
        // Only actual headings are mapped by position. Generic cards, labels,
        // prices, buttons and body copy must NEVER be mistaken for a heading.
        if (clicked.closest("#categories h2")) target = "categories";
        else if (clicked.closest("#products h2")) target = "products";
        else if (clicked.closest("#products a, #products article")) target = "productCardStyle";
        else if (clicked.closest("#categories a")) target = "categoryLayout";
        else if (clicked.closest("main h1")) target = "heroTitle";
        else if (clicked.closest("header img")) target = "logo";
        else if (clicked.closest("#categories > div > p")) target = "categoryEyebrow";
        else if (clicked.closest("#products > div > p")) target = "productEyebrow";
      }
      if (!target) return;
      event.preventDefault();
      event.stopPropagation();
      window.parent.postMessage({ type: "RUKN_VISUAL_SELECT", target }, window.location.origin);
    }

    document.addEventListener("click", handleBuilderSelection, true);

    window.addEventListener(
      "message",
      handleMessage,
    );

    return () => {
      window.removeEventListener(
        "message",
        handleMessage,
      );

      clearHighlight();
      document.removeEventListener("click", handleBuilderSelection, true);
    };
  }, []);

  return (
    <style>
      {`
        @keyframes ofoq-builder-highlight-pulse {
          0% {
            box-shadow:
              0 0 0 0 rgba(49, 90, 73, .42);
          }

          70% {
            box-shadow:
              0 0 0 12px rgba(49, 90, 73, 0);
          }

          100% {
            box-shadow:
              0 0 0 0 rgba(49, 90, 73, 0);
          }
        }

        .${HIGHLIGHT_CLASS} {
          position: relative !important;
          z-index: 50 !important;

          outline:
            3px solid #315a49 !important;

          outline-offset:
            5px !important;

          border-radius:
            4px;

          animation:
            ofoq-builder-highlight-pulse
            1.05s
            ease-out
            1;
        }
      `}
    </style>
  );
}