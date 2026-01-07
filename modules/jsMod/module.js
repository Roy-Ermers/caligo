import {
    defineBlock,
    defineCubeModel
} from '@core/blocks';


// Block with custom textures object
defineBlock("grass_block", () => {
    defineCubeModel({
        weight: 5,
        textures: {
            north: Array.from({length: 6}, (_, index) => `grass_top_${index + 1}`),
            south: "grass_block_top",
            east: "dirt",
            west: "dirt",
            up: "podzol_top",
            down: "podzol_top"
        }
    });
});

