import {
    defineBlock,
    defineCubeModel
} from '@core/blocks';


// Block with custom textures object
defineBlock("block", () => {
    defineCubeModel({
        weight: 5,
        textures: {
            north: "grass_block_top",
            south: "grass_block_top",
            east: "dirt",
            west: "dirt",
            up: "podzol_top",
            down: "podzol_top"
        }
    });
});

