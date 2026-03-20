# Commerce & CSA API Reference

The Commerce API serves as the primary gateway for economic logic within FarmOS. It handles bulk, high-margin logistics like full season Community Supported Agriculture (CSA), alongside asynchronous fulfillment workflows like sourdough orders mapping to `Hearth` bakes.

> **Gateway URL:** `http://localhost:5050/api/commerce`
> **Internal Service:** `FarmOS.Commerce.API`

---

## 📅 CSA Seasons
A CSA Season groups multiple Share Definitions (Half, Full) over a scheduled date range with pre-defined pickup locations.

### **Create Season**
**`POST /seasons`**
Initializes a new season blueprint.

**Request Body:**
```json
{
  "year": 2024,
  "name": "Summer 2024 Market Share",
  "startDate": "2024-05-01",
  "endDate": "2024-10-31",
  "shares": [
    {
      "size": 0, // Full
      "price": 600.00,
      "totalWeeks": 24,
      "includedCategories": ["Vegetables", "Sourdough", "Flower Bouquets"]
    }
  ]
}
```
**Response:** `201 Created`
```json
{ "id": "uuid-here" }
```

### **Schedule Pickup**
**`POST /seasons/{id}/pickups`**
Appends an active pickup point to the season roster.

**Request Body:**
```json
{
  "pickup": {
    "date": "2024-05-01",
    "timeWindow": "09:00-12:00",
    "location": "Broad Street Market",
    "maxSlots": null
  }
}
```
**Response:** `204 No Content`

---

## 👩‍🌾 CSA Members
Members represent humans who have purchased a share attached to a season. The EdgePortal interacts heavily with this domain layer.

### **Register Member**
**`POST /members`**

**Request Body:**
```json
{
  "seasonId": "season-uuid-here",
  "contact": {
    "name": "Jane Doe",
    "email": "jane@example.com",
    "phone": "555-0102",
    "preferredContact": "Email"
  },
  "shareType": 0,    // Full
  "method": 0        // 0: Pickup, 1: Delivery
}
```
**Response:** `201 Created`

### **Record Payment**
**`POST /members/{id}/payments`**

**Request Body:**
```json
{
  "amount": 300.00,
  "paymentMethod": "Stripe",
  "reference": "ch_3Mqw..."
}
```
**Response:** `204 No Content`

---

## 🛒 Direct Orders
Handles asynchronous, point-in-time orders. E.g., someone ordered a customized kombucha and sourdough loaf for Saturday pickup via the EdgePortal.

### **Create Order**
**`POST /orders`**

**Request Body:**
```json
{
  "customerName": "John Smith",
  "items": [
    {
      "productName": "Jun Kombucha - Strawberry Basil",
      "category": "Ferments",
      "qty": { "value": 2, "unit": "bottle", "type": "count" },
      "unitPrice": 8.00,
      "notes": "Ensure high carbonation"
    }
  ],
  "method": 0 // 0: Pickup
}
```
**Response:** `201 Created`

### **Pack Order**
**`POST /orders/{id}/pack`**
Marks the order as prepared (items assembled, boxed, cold-stored).

**Response:** `204 No Content`

### **Fulfill Order**
**`POST /orders/{id}/fulfill`**
Marks the order as picked up by the customer or delivered to their door (closing the lifecycle).

**Response:** `204 No Content`
