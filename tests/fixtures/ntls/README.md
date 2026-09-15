# NTLS / 国密 TLS captures

Version-controlled handshake bytes captured from real 国密 TLS endpoints,
consumed by `DevTrove.Crypto.Tls` (`0.4.0`) and the SM2 fingerprint
fixtures.

## Conventions

- `<host>-<yyyy-mm-dd>.bin` — raw record bytes, exactly as observed
- `<host>-<yyyy-mm-dd>.source.md` — provenance, capture tool, SHA-256

The current set is empty. Captures land here once `DevTrove.Crypto.Tls`
exists and the integration test list defines which sites to probe.
