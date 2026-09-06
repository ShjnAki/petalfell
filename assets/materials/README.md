# Surface detail sources

`mineral-detail.png` is a neutral grayscale detail source generated with the
built-in image-generation tool on 2026-09-06. It is used as data for fine
mineral variation and relief, not as a colour texture or a world image.
The original generated pixels are preserved. The shader mirrors UVs for
continuous boundaries and controls colour, strength, scale and distance fade.
Runtime sunlight and moonlight shade the geometry and relief normally.
`WorldMaterials` generates mipmaps once when the imported texture has none,
then shares the result across voxel and sculpture materials. This is runtime
filtering data; the source pixels remain unchanged. Snow samples a coarser band
before shaping its broad blue glaze so magnified pores do not become speckle.

## Generation prompt

Use case: stylized-concept. Asset type: one seamless grayscale material-detail texture for a realtime pastel voxel game. Produce a square 1024x1024 flat orthographic texture, filling the entire image edge to edge. Subject: pale chalky limestone surface weathering, rendered as a NEUTRAL GRAYSCALE relief/variation mask with a middle gray average. Match the fine restrained material richness of miniature pastel voxel cliff stone: many small shallow chipped mineral flakes with irregular angular stepped contours, some soft worn patches, tiny pinprick pores, and a few very fine hairline fractures. Hierarchy: quiet large areas, overlapping medium flakes, sparse tiny grain, all with subtle low to medium contrast. No bricks, no mortar, no regular grid, no large stone slabs, no cobblestones, no moss, no vegetation. No perspective, no geometry object, no scene, no floor horizon, no borders, no text, no lettering, no labels, no lighting direction, no cast shadows, no ambient vignette, no specular highlights. This is an unlit material data image, not a render of a slab. Tile seamlessly on all four edges. Nothing very dark or white; most values between 90 and 175 out of 255. Fine faceted painterly mineral structure, legible and carefully spaced, no blur haze, no photoreal grunge.
