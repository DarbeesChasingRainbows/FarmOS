# Local Discovery & Visibility Guide

> **Date**: 2026-03-23
> **Context**: Now that FarmOS exposes structured data, MCP tools, and UCP checkout, these manual registrations connect the farm to local directories and AI shopping platforms.

## 1. Georgia Grown (State Program)

**Website**: https://georgiagrown.com / https://georgiagrownmembers.com

**What it provides**:
- Listing in the Georgia Grown directory (searchable by county, product type)
- Permission to use the Georgia Grown logo on packaging and website
- Inclusion in Georgia Grown marketing campaigns and events
- Access to Georgia Grown farmers markets

**Registration steps**:
1. Apply at georgiagrownmembers.com with farm address (must be Georgia-based)
2. Provide list of Georgia-grown products (cut flowers, mushrooms, eggs, honey, etc.)
3. Upload farm photos and description
4. Pay annual membership fee
5. Once approved, add Georgia Grown badge to website and product labels

**FarmOS integration**: Link the Georgia Grown listing URL to:
- `GET /api/marketplace/structured-data` for product details
- `/.well-known/ucp` for AI agent purchasing

---

## 2. Community Farmers Markets (Atlanta Metro)

**Website**: https://cfmatl.org

**What it provides**:
- Booth at Atlanta-area community farmers markets
- Listing in the CFM online vendor directory
- Access to SNAP/EBT token program at markets

**Registration steps**:
1. Apply as a vendor on cfmatl.org
2. Provide product list, farm certifications, and insurance
3. Select preferred market locations and days
4. Complete food safety training if selling prepared foods (sourdough, kombucha)

**FarmOS integration**: Market schedule can feed into the `openingHoursSpecification` in structured data endpoint.

---

## 3. Food Well Alliance

**Website**: https://foodwellalliance.org

**What it provides**:
- County-based local food finder listing
- Connection to community garden networks
- Grant opportunities for local food systems

**Registration steps**:
1. Register farm on the Food Well Alliance directory
2. Specify county/region and product categories
3. Add seasonal availability information

---

## 4. USDA Local Food Directory

**Website**: https://www.usdalocalfoodportal.com

**What it provides**:
- National listing in USDA's local food directory
- Visibility to consumers searching for local farms
- Integration with USDA food hub networks

**Registration steps**:
1. Create account on USDA Local Food Portal
2. Enter farm details: name, address, contact, website
3. Select marketing channels: farm stand, CSA, farmers market, online sales
4. List product categories
5. Indicate if farm accepts SNAP/EBT (we do)

---

## 5. Google Business Profile

**Website**: https://business.google.com

**What it provides**:
- "Farm near me" search results placement
- Google Maps listing with hours, photos, reviews
- Direct link to website and ordering
- Knowledge panel in Google Search

**Registration steps**:
1. Claim or create business at business.google.com
2. Set business category to "Farm" + "Farm Stand"
3. Add address, phone, website URL
4. Set operating hours (Wed-Sat 9am-5pm per structured data)
5. Upload farm photos (flowers, products, farm stand)
6. Add products with prices (syncs with inventory)
7. Enable messaging and Q&A
8. Add "Place an order" link pointing to the storefront

**FarmOS integration**:
- Link website to storefront (when Deno Fresh frontend is built)
- Google can crawl `/api/marketplace/structured-data` for rich results
- Future: Google Shopping integration via UCP

---

## 6. AI Shopping Platform Registration

### Claude (Anthropic)
- MCP server at `/mcp` is natively compatible
- Register farm's MCP endpoint when Anthropic opens merchant directory
- Tools: `ListProducts`, `SearchProducts`, `PlaceOrder`, `BrowseCsaItems`

### Google Gemini / Shopping
- UCP discovery at `/.well-known/ucp` follows Google's co-developed standard
- Register UCP endpoint when Google opens merchant onboarding
- Supports: catalog browsing, checkout sessions, Google Pay

### ChatGPT (OpenAI)
- Expose MCP endpoint (OpenAI has announced MCP support)
- Alternatively, provide UCP endpoint for standardized checkout
- Schema.org structured data at `/api/marketplace/structured-data` provides fallback discovery

---

## API Endpoints Summary

| Endpoint | Purpose | Who consumes it |
|---|---|---|
| `GET /api/marketplace/structured-data` | Schema.org JSON-LD | Google, Bing, search crawlers |
| `GET /.well-known/ucp` | UCP discovery profile | AI shopping agents (Google, etc.) |
| `POST /mcp` | MCP tool server | Claude, ChatGPT, AI agents |
| `GET /ucp/catalog/items` | UCP product catalog | UCP-compatible agents |
| `POST /ucp/checkout-sessions` | UCP checkout | AI checkout flows |
| `GET /api/commerce/inventory` | REST inventory | Storefront, mobile app |

---

## Priority Order

1. **Google Business Profile** — highest impact for local visibility, free
2. **Georgia Grown** — state program credibility, logo usage rights
3. **USDA Local Food Directory** — national visibility, SNAP/EBT highlighting
4. **Community Farmers Markets** — direct sales channel for Atlanta metro
5. **Food Well Alliance** — community connections, grant opportunities
6. **AI Shopping Platforms** — register as platforms open merchant directories
