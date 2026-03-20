FarmOS: Edge Appliance & Cooperative Cloud Specification

1. System Mission & Economic Model
FarmOS is a decentralized, multi-tenant agricultural management platform operating on an open-core, tiered economic model. It equips local, sovereign farms with enterprise-grade operational software at zero cost (via the local edge appliance), while funding the mission through a paid SaaS tier. The paid tier connects these vetted producers to affluent consumers via a unified global marketplace, effectively funding food security initiatives and agrarian education for the marginalized.

2. Global Architecture (Edge-to-Cloud)
The platform abandons the pure-cloud SaaS model in favor of a Local-First (Edge-to-Cloud) Architecture.

The Sovereign Edge: A physical hardware appliance located on the farm. It operates entirely offline, managing daily chores, livestock rotations, and HACCP logs with zero latency.

The Cooperative Cloud: A managed cloud environment handling global e-commerce routing, B2B cooperative trading, cross-tenant telemetry, and identity verification.

The Synchronization Engine: Edge nodes utilize an Outbox pattern via RabbitMQ. When an internet connection is detected, immutable domain events (e.g., OrderPacked, GrazingStarted) are synchronized asynchronously to the cloud hub.

3. The Edge Appliance (Hardware & Security)
To prevent the proliferation of malicious mirror networks and protect the integrity of the cooperative, the local software is delivered exclusively via a locked-down physical appliance ("Farm-in-a-Box").

Hardware Baseline: Fanless industrial mini-PC (e.g., Intel NUC) capable of running a lightweight Kubernetes (K3s) cluster.

Layer 1: Full Disk Encryption: The primary NVMe drive is encrypted using LUKS. Decryption is bound cryptographically to the motherboard's Trusted Platform Module (TPM). If the drive is removed, the data is unreadable.

Layer 2: Secure Boot: The BIOS is locked and UEFI Secure Boot is enforced, preventing the execution of unauthorized live USB operating systems.

Layer 3: Service Isolation: The backend services run under isolated, non-login Linux accounts (ProtectSystem=strict). The filesystem is read-only, except for explicit ArangoDB and RabbitMQ event stream directories.

4. The Local Software Stack (Domain-Driven Design)
The software running on the edge appliance strictly adheres to CQRS and Event Sourcing patterns.

The Command Pipeline: Built using C# 13 and MediatR. Commands validate intentions and append immutable EventEnvelope records to ArangoDB. Binaries are compiled using Native AOT to strip intermediate language metadata and obfuscate the proprietary domain logic.

The Rules Engine: Domain invariants (e.g., calculating safe pH thresholds for kombucha or 45-day pasture rest periods) are written as pure, testable functions in F#.

The Read Projectors: Background workers (IHostedService) listen to the ArangoDB event stream and execute idempotent AQL UPSERT queries to build flattened, highly optimized read models for the UI.

The Micro-Frontends: The user interfaces (FieldOps, HearthOS, and ApiaryOS) are built using the Deno 2 runtime and Deno Fresh 2.2.2 framework. They utilize an Islands Architecture (Preact) to deliver server-side rendered pages that remain perfectly functional in low-connectivity environments.

5. The Cooperative Cloud (Multi-Tenant SaaS)
The cloud infrastructure acts as the aggregator and public storefront, shifting from a single-farm database to a partitioned global pool.

Tenant Isolation: Every event appended to the global event store is tagged with a TenantId. The ArangoDB cloud cluster utilizes persistent indexes on TenantId across all collections to ensure absolute data isolation between cooperative members.

The Sovereign Marketplace (EdgePortal): A globally accessible Deno Fresh web application. It queries the aggregated Commerce.API to display available CSA subscriptions and farm products from the nearest vetted farms, acting as the primary revenue engine for the network.

The Guild Context: A new bounded context managing cross-farm relationships, B2B wholesale trading, and peer-to-peer vouching protocols.

6. Over-The-Air (OTA) Updates & Deployment
Updates to the edge appliances bypass inbound networking entirely, utilizing a zero-trust, pull-based GitOps workflow.

The Controller: FluxCD (or Rancher Fleet) runs inside the local K3s cluster, polling the central container registry every 15 minutes over outbound HTTPS (Port 443).

Cryptographic Verification: Continuous Integration (CI) pipelines compile the C# and Deno artifacts, build the Docker containers, and sign them using Cosign. The local K3s Kyverno policy engine verifies this cryptographic signature before allowing any new image to execute.

Zero-Downtime Rollouts: K3s health probes ensure the new API container is fully operational before silently rerouting traffic and terminating the old version. Crash loops automatically trigger rollbacks.

7. The Excommunication Protocol
If a farm is caught violating the cooperative's sovereign input standards (e.g., reselling industrial products), they face immediate digital exile.

The Mechanism: The farm's TenantId is revoked at the Cloud Hub API Gateway.

The Result: The RabbitMQ outbox synchronization is permanently blocked. The farm's local hardware appliance continues to function perfectly for their internal operations (preserving their data sovereignty), but they are instantly eradicated from the public EdgePortal marketplace and internal B2B trading networks.