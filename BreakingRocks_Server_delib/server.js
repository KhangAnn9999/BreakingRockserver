const express = require('express');
const bodyParser = require('body-parser');
const mongoose = require('mongoose');
const cors = require('cors');
const bcrypt = require('bcrypt');
const jwt = require('jsonwebtoken');

const User = require('./models/User');
const Player = require('./models/Player');
const Item = require('./models/Item');

require('dotenv').config();

const app = express();
app.use(cors());
app.use(bodyParser.json());

// Kết nối Database
mongoose.connect(process.env.MONGO_URI)
  .then(() => console.log("✅ Đã kết nối MongoDB"))
  .catch(err => console.error("❌ Lỗi kết nối:", err));

// Middleware xác thực JWT
function authenticateToken(req, res, next) {
  const authHeader = req.headers['authorization'];
  if (!authHeader) return res.status(401).json({ error: 'Chưa đăng nhập' });
  const token = authHeader.split(' ')[1];
  if (!token) return res.status(401).json({ error: 'Token khuyết' });
  jwt.verify(token, process.env.JWT_SECRET, (err, user) => {
    if (err) return res.status(403).json({ error: 'Token không hợp lệ' });
    req.user = user;
    next();
  });
}

// REGISTER (User + Player)
app.post('/api/auth/register', async (req, res) => {
  try {
    const { username, email, password } = req.body;
    if (!username || !email || !password)
      return res.status(400).json({ error: 'Thiếu trường dữ liệu' });

    let existing = await User.findOne({ $or: [{ email }, { username }] });
    if (existing) return res.status(400).json({ error: 'Email hoặc Username đã tồn tại' });

    const hash = await bcrypt.hash(password, 10);
    const user = await User.create({ username, email, password: hash });
    const player = await Player.create({ user: user._id, inventory: [], money: 0 });

    // Optionally embed playerId in user
    user.player = player._id;
    await user.save();

    res.json({ message: "Đăng ký thành công", userId: user._id, playerId: player._id });
  } catch (e) {
    res.status(500).json({ error: "Lỗi server đăng ký" });
  }
});

// LOGIN
app.post('/api/auth/login', async (req, res) => {
  try {
    const { usernameOrEmail, password } = req.body;
    if (!usernameOrEmail || !password)
      return res.status(400).json({ error: 'Thiếu trường dữ liệu' });

    let user =
      await User.findOne({ email: usernameOrEmail }) ||
      await User.findOne({ username: usernameOrEmail });
    if (!user) return res.status(404).json({ error: "Tài khoản không tồn tại" });

    const match = await bcrypt.compare(password, user.password);
    if (!match) return res.status(401).json({ error: "Sai mật khẩu" });

    const token = jwt.sign(
      { userId: user._id, username: user.username, playerId: user.player },
      process.env.JWT_SECRET,
      { expiresIn: "12h" }
    );
    res.json({ token, user: { userId: user._id, username: user.username, playerId: user.player } });
  } catch (e) {
    res.status(500).json({ error: "Lỗi server đăng nhập" });
  }
});

// GET ALL ITEMS (optional, for debug)
app.get('/api/items', async (req, res) => {
  try {
    const items = await Item.find();
    res.json(items);
  } catch (e) {
    res.status(500).json({ error: 'Lỗi truy vấn items' });
  }
});

// GAME: BREAK STONE API
app.post('/api/game/break-stone', authenticateToken, async (req, res) => {
  try {
    // Tìm player theo user
    const playerId = req.user.playerId || req.body.playerId;
    const player = await Player.findById(playerId);
    if (!player) return res.status(404).json({ error: "Không tìm thấy player" });

    if (player.inventory.length >= 20)
      return res.status(400).json({ error: 'Túi đồ đầy' });

    // Lấy list items từ collection Items
    const items = await Item.find();
    if (!items || items.length === 0)
      return res.status(500).json({ error: 'Chưa có items nào trong DB' });

    // Tạo cumulative bảng tỉ lệ
    const totalChance = items.reduce((sum, s) => sum + s.chance, 0);
    const rand = Math.random() * totalChance;
    let acc = 0;
    let resultStone = null;
    for (let s of items) {
      acc += s.chance;
      if (rand < acc) {
        resultStone = s;
        break;
      }
    }
    if (!resultStone) resultStone = items[items.length - 1];

    // Thêm vào inventory và lưu
    player.inventory.push({
      item: resultStone._id,
      itemName: resultStone.type,
      rarity: resultStone.type,
      value: resultStone.value
    });
    await player.save();

    res.json({
      message: `Bạn nhận được đá ${resultStone.type}!`,
      item: {
        id: resultStone._id,
        type: resultStone.type,
        value: resultStone.value
      },
      inventoryCount: player.inventory.length
    });
  } catch (e) {
    res.status(500).json({ error: "Lỗi server break-stone" });
  }
});

app.listen(process.env.PORT || 5000, () => {
  console.log('🚀 Server is running!');
});