# Docker, Helm and Kubernetes identity cutover

The OSDC rename changes deployment identities but does not change the public routes or the existing database claim.

| Purpose | Previous identity | New identity |
| --- | --- | --- |
| Service Docker repository and Helm chart/release | `norcedrillingfieldservice` | `osdcdrillingfieldservice` |
| Service Kubernetes resources and cluster DNS | `norcedrillingfieldservice` | `osdcfieldservice` |
| WebApp Docker repository and Helm chart/release | `norcedrillingfieldwebappclient` | `osdcdrillingfieldwebappclient` |
| WebApp Kubernetes resources and cluster DNS | `norcedrillingfieldwebappclient` | `osdcfieldwebappclient` |
| Database PVC | `field-claim` | `field-claim` (unchanged) |
| REST route | `/Field/api` | `/Field/api` (unchanged) |
| Web route | `/Field/webapp` | `/Field/webapp` (unchanged) |

There are no compatibility Services or aliases. Workloads must use `http://osdcfieldservice/` after the cutover.

## Before changing a cluster

1. Push and verify `digiwells/osdcdrillingfieldservice:stable` and `digiwells/osdcdrillingfieldwebappclient:stable`.
2. Export all fields through the batch backup API and retain the verified JSON backup outside the cluster.
3. Save the current Helm values for both old releases.
4. Prepare updated images/configuration for every in-cluster consumer listed below so they use `http://osdcfieldservice/`. Deploy those updates after the renamed service is available.
5. Stop the old WebApp through Helm and release its public route:

   ```powershell
   helm upgrade norcedrillingfieldwebappclient `
     .\WebApp\charts\osdcdrillingfieldwebappclient `
     --kube-context <context> `
     -n default `
     -f <saved-webapp-values.yaml> `
     --set replicaCount=0 `
     --set ingress.enabled=false `
     --set-string nameOverride=norcedrillingfieldwebappclient `
     --set-string fullnameOverride=norcedrillingfieldwebappclient `
     --set-string image.repository=docker.io/digiwells/norcedrillingfieldwebappclient
   ```

6. Upgrade the old service release once with the new chart while retaining its old resource names. This stops the old writer, releases its route, and, critically, records the PVC retention policy in Helm's release manifest:

   ```powershell
   helm upgrade norcedrillingfieldservice `
     .\Service\charts\osdcdrillingfieldservice `
     --kube-context <context> `
     -n default `
     -f <saved-service-values.yaml> `
     --set replicaCount=0 `
     --set ingress.enabled=false `
     --set persistence.enabled=true `
     --set-string persistence.claimName=field-claim `
     --set-string nameOverride=norcedrillingfieldservice `
     --set-string fullnameOverride=norcedrillingfieldservice `
     --set-string image.repository=docker.io/digiwells/norcedrillingfieldservice
   ```

7. Verify that the old service has no running pod and that the retained claim is annotated:

   ```powershell
   kubectl --context <context> get pods -n default `
     -l app.kubernetes.io/instance=norcedrillingfieldservice

   kubectl --context <context> get pvc field-claim -n default `
     -o jsonpath='{.metadata.annotations.helm\.sh/resource-policy}'
   ```

The second command must print `keep`. Do not run old and new Field service pods concurrently. The new service chart also uses the `Recreate` deployment strategy because the shared volume contains a SQLite database.

## Install the renamed releases

Use the saved environment-specific values, but override any old identity/image settings. The new service must adopt the existing claim:

```powershell
helm upgrade --install osdcdrillingfieldservice `
  .\Service\charts\osdcdrillingfieldservice `
  --kube-context <context> `
  -n default `
  -f <saved-service-values.yaml> `
  --set-string nameOverride=osdcdrillingfieldservice `
  --set-string fullnameOverride=osdcfieldservice `
  --set-string image.repository=docker.io/digiwells/osdcdrillingfieldservice `
  --set-string image.tag=stable `
  --set-string persistence.existingClaim=field-claim

helm upgrade --install osdcdrillingfieldwebappclient `
  .\WebApp\charts\osdcdrillingfieldwebappclient `
  --kube-context <context> `
  -n default `
  -f <saved-webapp-values.yaml> `
  --set-string nameOverride=osdcdrillingfieldwebappclient `
  --set-string fullnameOverride=osdcfieldwebappclient `
  --set-string image.repository=docker.io/digiwells/osdcdrillingfieldwebappclient `
  --set-string image.tag=stable
```

After the renamed service is ready, deploy the prepared consumer configuration changes and then install the renamed WebApp. Remove the old releases only after verifying the new pods, routes, field count, field IDs, projection IDs, and a coordinate conversion. The `keep` annotation prevents Helm from deleting `field-claim` when the old service release is removed.

## In-cluster consumers

At the time of the rename, these production configurations referenced `http://norcedrillingfieldservice/` and require coordinated updates and redeployment:

- Cluster WebApp
- GeologicalProperties WebApp
- Rig WebApp
- Simulator4nDOF WebApp
- Trajectory Service and WebApp
- Well WebApp
- WellBore WebApp

Search all deployment repositories and cluster-specific values once more immediately before each cutover; this list is an inventory, not a compatibility mechanism.
