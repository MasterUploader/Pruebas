curl -k -X POST "https://localhost:8443/davivienda-tnp/api/v1/authorization/manual" \
  -H "Content-Type: application/json" \
  -d '{
    "GetAuthorizationManual": {
      "pMerchantID": "4001021",
      "pTerminalID": "P0055468",
      "pPrimaryAccountNumber": "5413330057004039",
      "pDateExpiration": "2512",
      "pCVV2": "000",
      "pAmount": "10000",
      "pSystemsTraceAuditNumber": "000002"
    }
  }'
