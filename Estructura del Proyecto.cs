{
  "CashBalanceResponse": {
    "Result": "0",
    "CountryId": "ES",
    "Balances": {
      "Balance": [
        {
          "BalanceType": "Total",
          "DeviceCode": "QA0013000170",
          "DateBalance": "2022-09-08T09:40:00+01:00",
          "Currency": [
            {
              "Code": "EUR",
              "Amount": 1234000,
              "Denominations": {
                "Denomination": [
                  { "Value": 100, "Quantity": 0, "Amount": 0, "Type": "COIN" },
                  { "Value": 1000, "Quantity": 62, "Amount": 62000, "Type": "NOTE" },
                  { "Value": 2000, "Quantity": 554, "Amount": 1108000, "Type": "NOTE" },
                  { "Value": 5000, "Quantity": 30, "Amount": 150000, "Type": "NOTE" },
                  { "Value": 10000, "Quantity": 1114, "Amount": 11140000, "Type": "NOTE" }
                ]
              }
            }
          ]
        }
      ]
    },
    "Transactions": {
      "Transaction": [
        {
          "ActualId": "20220907100500_QA0013000170_CASHIN_1",
          "TransactonDate": "2022-09-07T09:05:00+01:00",
          "ServicePoint": null,
          "ReceiptNumber": "710",
          "Currency": [
            {
              "Code": "EUR",
              "Amount": 440000,
              "Denominations": {
                "Denomination": [
                  { "Value": 2000, "Quantity": 20, "Amount": 40000, "Type": "NOTE" },
                  { "Value": 10000, "Quantity": 40, "Amount": 400000, "Type": "NOTE" }
                ]
              },
              "CashierId": "003",
              "CashierName": "usuario_prosegur_rest"
            }
          ],
          "TipoTrans": "CASHIN"
        }
      ]
    }
  }
}
