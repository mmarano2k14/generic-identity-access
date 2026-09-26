"use client";

import { useEffect, useId, useRef, useState } from "react";
import type { AdminEntityReferenceKind } from "../contracts/AdminEntityReferenceKind";
import type { AdminEntityReferenceOption } from "../contracts/AdminEntityReferenceOption";

type Props = {
  readonly label: string;
  readonly name: string;
  readonly kind: AdminEntityReferenceKind;
  readonly tenantId?: string;
  readonly defaultValue?: string;
  readonly required?: boolean;
  readonly hint?: string;
  readonly placeholder?: string;
  readonly emptyLabel?: string;
  readonly disabled?: boolean;
  readonly excludeIds?: readonly string[];
  readonly onSelectedIdChange?: (selectedId: string) => void;
};

const minimumSearchLength = 3;
const debounceMilliseconds = 250;

/** Shared server-backed relation selector. It submits only the stable entity identifier. */
export function AdminEntityAutocomplete({
  label,
  name,
  kind,
  tenantId,
  defaultValue = "",
  required = false,
  hint,
  placeholder = "Type at least 3 characters",
  emptyLabel,
  disabled = false,
  excludeIds = [],
  onSelectedIdChange,
}: Props) {
  const inputId = useId();
  const listboxId = useId();
  const hintId = useId();
  const statusId = useId();
  const inputRef = useRef<HTMLInputElement>(null);
  const [selectedId, setSelectedId] = useState(defaultValue);
  const [selectedDisplay, setSelectedDisplay] = useState("");
  const [query, setQuery] = useState(defaultValue);
  const [options, setOptions] = useState<readonly AdminEntityReferenceOption[]>([]);
  const [open, setOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(0);
  const [loading, setLoading] = useState(false);
  const [searchError, setSearchError] = useState<string | null>(null);
  const excluded = new Set(excludeIds);
  const visibleOptions = options.filter((option) => !excluded.has(option.id));
  const hasResolvedSelection = selectedId.length > 0 && selectedDisplay.length > 0 && query === selectedDisplay;
  const activeOptionId = open && visibleOptions[activeIndex] ? `${listboxId}-option-${activeIndex}` : undefined;

  useEffect(() => {
    const invalidSelection = query.trim().length > 0 && selectedId.length === 0;
    inputRef.current?.setCustomValidity(
      invalidSelection || (required && selectedId.length === 0)
        ? "Select a listed record by display name or ID."
        : "",
    );
  }, [query, required, selectedId]);

  useEffect(() => {
    const value = query.trim();
    if (disabled || hasResolvedSelection) {
      setLoading(false);
      setSearchError(null);
      return undefined;
    }
    if (value.length < minimumSearchLength) {
      setOptions([]);
      setLoading(false);
      setSearchError(null);
      return undefined;
    }

    const controller = new AbortController();
    const timer = window.setTimeout(async () => {
      setLoading(true);
      setSearchError(null);
      try {
        const parameters = new URLSearchParams({ kind, q: value });
        if (tenantId) parameters.set("tenantId", tenantId);
        const response = await fetch(`/api/identity/entity-references?${parameters.toString()}`, {
          method: "GET",
          cache: "no-store",
          signal: controller.signal,
          headers: { Accept: "application/json" },
        });
        if (!response.ok) {
          setOptions([]);
          setSearchError(response.status === 403 ? "You are not allowed to search these records." : "Search is temporarily unavailable.");
          return;
        }
        const body: unknown = await response.json();
        if (!Array.isArray(body)) throw new Error("Invalid entity-reference response.");
        const next = body.filter(isReferenceOption).slice(0, 20);
        setOptions(next);
        setActiveIndex(0);

        if (selectedId.length > 0 && selectedDisplay.length === 0) {
          const resolved = next.find((option) => option.id === selectedId);
          if (resolved) {
            setSelectedDisplay(resolved.displayName);
            setQuery(resolved.displayName);
            setOptions([]);
            setOpen(false);
          }
        }
      } catch (error) {
        if (error instanceof DOMException && error.name === "AbortError") return;
        setOptions([]);
        setSearchError("Search is temporarily unavailable.");
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    }, debounceMilliseconds);

    return () => {
      window.clearTimeout(timer);
      controller.abort();
    };
  }, [disabled, hasResolvedSelection, kind, query, selectedDisplay, selectedId, tenantId]);

  const select = (option: AdminEntityReferenceOption) => {
    setSelectedId(option.id);
    onSelectedIdChange?.(option.id);
    setSelectedDisplay(option.displayName);
    setQuery(option.displayName);
    setOptions([]);
    setOpen(false);
    setActiveIndex(0);
  };

  const clear = () => {
    setSelectedId("");
    onSelectedIdChange?.("");
    setSelectedDisplay("");
    setQuery("");
    setOptions([]);
    setOpen(false);
    setActiveIndex(0);
    setSearchError(null);
  };

  const status = hasResolvedSelection
    ? `${selectedDisplay} selected.`
    : loading
      ? "Searching…"
      : searchError
        ? searchError
        : query.trim().length < minimumSearchLength
          ? `Type at least ${minimumSearchLength} characters to search.`
          : visibleOptions.length === 0
            ? "No matching records."
            : `${visibleOptions.length} matching record${visibleOptions.length === 1 ? "" : "s"}.`;

  return (
    <div className="ia-field ia-entity-autocomplete">
      <label className="ia-field-label" htmlFor={inputId}>{label}</label>
      <input type="hidden" name={name} value={selectedId} />
      <div className="ia-entity-autocomplete-control">
        <input
          ref={inputRef}
          className="ia-input"
          id={inputId}
          type="text"
          value={query}
          placeholder={placeholder}
          autoComplete="off"
          disabled={disabled}
          required={required}
          role="combobox"
          aria-autocomplete="list"
          aria-expanded={open}
          aria-controls={listboxId}
          aria-activedescendant={activeOptionId}
          aria-describedby={`${statusId}${hint ? ` ${hintId}` : ""}`}
          onFocus={() => {
            if (!hasResolvedSelection) setOpen(true);
          }}
          onChange={(event) => {
            const value = event.currentTarget.value;
            setQuery(value);
            if (value !== selectedDisplay) {
              if (selectedId.length > 0) onSelectedIdChange?.("");
              setSelectedId("");
              setSelectedDisplay("");
            }
            setOpen(true);
            setActiveIndex(0);
          }}
          onKeyDown={(event) => {
            if (event.key === "ArrowDown") {
              event.preventDefault();
              setOpen(true);
              setActiveIndex((index) => Math.min(index + 1, Math.max(visibleOptions.length - 1, 0)));
            } else if (event.key === "ArrowUp") {
              event.preventDefault();
              setActiveIndex((index) => Math.max(index - 1, 0));
            } else if (event.key === "Enter" && open && visibleOptions[activeIndex]) {
              event.preventDefault();
              select(visibleOptions[activeIndex]);
            } else if (event.key === "Escape") {
              setOpen(false);
            }
          }}
          onBlur={() => window.setTimeout(() => setOpen(false), 120)}
        />
        {selectedId || (!required && query) ? (
          <button className="ia-entity-autocomplete-clear" type="button" onClick={clear} aria-label={`Clear ${label}`} disabled={disabled}>×</button>
        ) : null}
      </div>
      {open ? (
        <div className="ia-entity-autocomplete-list" id={listboxId} role="listbox">
          {!required && emptyLabel ? (
            <button className="ia-entity-autocomplete-option" type="button" role="option" aria-selected={selectedId === ""} onMouseDown={(event) => event.preventDefault()} onClick={clear}>
              <strong>{emptyLabel}</strong>
            </button>
          ) : null}
          {visibleOptions.map((option, index) => (
            <button
              className={`ia-entity-autocomplete-option${index === activeIndex ? " ia-entity-autocomplete-option-active" : ""}`}
              id={`${listboxId}-option-${index}`}
              type="button"
              role="option"
              aria-selected={selectedId === option.id}
              key={option.id}
              onMouseDown={(event) => event.preventDefault()}
              onMouseEnter={() => setActiveIndex(index)}
              onClick={() => select(option)}
            >
              <span><strong>{option.displayName}</strong>{option.description ? <small>{option.description}</small> : null}</span>
              <code>{option.id}</code>
            </button>
          ))}
          <p className="ia-entity-autocomplete-empty" id={statusId} role="status" aria-live="polite">{status}</p>
        </div>
      ) : <span className="ia-visually-hidden" id={statusId}>{status}</span>}
      {selectedId ? <small className="ia-entity-autocomplete-selected">Selected ID: <code>{selectedId}</code></small> : null}
      {hint ? <small className="ia-field-hint" id={hintId}>{hint}</small> : null}
    </div>
  );
}

function isReferenceOption(value: unknown): value is AdminEntityReferenceOption {
  if (typeof value !== "object" || value === null) return false;
  const candidate = value as Record<string, unknown>;
  return typeof candidate.id === "string"
    && typeof candidate.displayName === "string"
    && (candidate.description === undefined || typeof candidate.description === "string");
}
