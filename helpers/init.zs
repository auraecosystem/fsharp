// 1. Bracket Handlers & Modifiers
var sword = <item:minecraft:diamond_sword>.withTag({display: {Name: 'Excalibur'}});
var copperTag = <tag:items:forge:ingots/copper>;
var lava = <fluid:minecraft:lava> * 1000;

// 2. Recipe Removal & Registration
recipes.remove("minecraft:stick");

recipes.addShaped("custom_excalibur", sword, [
    [<item:minecraft:diamond>],
    [copperTag],
    [<item:minecraft:stick>]
]);
