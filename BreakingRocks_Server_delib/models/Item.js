const mongoose = require('mongoose');

const ItemSchema = new mongoose.Schema({
    itemCode: { type: String, required: true, unique: true }, // ID định danh, VD: "da_01"
    itemName: { type: String, required: true },               // Tên hiển thị, VD: "Đá Cổ Đại"
    rarity: { type: String, default: "Common" },              // Độ hiếm
    chance: { type: Number, required: true },                 // Tỉ lệ rơi (0 - 100)
    value: { type: Number, default: 0 }                       // Giá bán lấy tiền
}, { timestamps: true });

module.exports = mongoose.model('Item', ItemSchema);