# Find

The `Find` API provides shortcut methods for retrieving various resources.

## Assets

### GlobalAsset

`T Find.GlobalAsset<T>()`

Retrieves the currently loaded `GlobalAsset` of the given type. If the asset of this type is not loaded, it will Assert. Guaranteed to not be null as a result.

`Find.GlobalAsset<T0>(out T0 assetA)`
`Find.GlobalAsset<T0, T1>(out T0 assetA, out T1 assetB)`
`Find.GlobalAsset<T0, T1, T2>(out T0 assetA, out T1 assetB, out T2 assetC)`

Retrieves the currently loaded `GlobalAsset` instances of the given types, and writes them to the output parameters. If any of the assets are not loaded, it will Assert. All outputs are guaranteed to not be null as a result.

`T Find.NamedAsset<T>(StringHash32 assetId)`

Retrieves the currently loaded `NamedAsset` with the given type and id. If no matching asset is loaded, it will Assert. Guaranteed not to be null as a result.