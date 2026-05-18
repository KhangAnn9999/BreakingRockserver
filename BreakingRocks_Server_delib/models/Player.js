const mongoose = require('mongoose');

const PlayerSchema = new mongoose.Schema({
    // Dòng này cực kỳ quan trọng: Nó liên kết Player với User
    user: { 
        type: mongoose.Schema.Types.ObjectId, 
        ref: 'User',
        required: true 
    },
    username: String, 
    money: { type: Number, default: 0 },
    health: { type: Number, default: 100 },
    stamina: { type: Number, default: 100 },
    inventory: [{
        itemName: String,
        rarity: String,
        value: Number
    }],
    storage: [{
        itemName: String,
        rarity: String,
        value: Number
    }]
}, { timestamps: true });

module.exports = mongoose.model('Player', PlayerSchema);