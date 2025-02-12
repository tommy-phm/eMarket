function changeQuantity(productId, amount) {
    let input = document.getElementById(`quantity-${productId}`);
    let currentValue = parseInt(input.value) || 1;
    let newValue = currentValue + amount;
    if (newValue < 1) newValue = 1;
    input.value = newValue;
}

function getCart() {
    let cart = localStorage.getItem("cart");
    return cart ? JSON.parse(cart) : [];
}

function saveCart(cart) {
    localStorage.setItem("cart", JSON.stringify(cart));
}

function addToCart(productId) {
    let quantity = parseInt(document.getElementById("quantity-" + productId).value);
    if (quantity <= 0) return;

    let cart = getCart();
    let index = cart.findIndex(item => item.productId === productId);

    if (index !== -1) {
        cart[index].quantity = quantity;
    } else {
        cart.push({ productId, quantity });
    }

    saveCart(cart);
    alert("Added to cart!");
}

function loadCart() {
    let cart = getCart();
    cart.forEach(item => {
        let input = document.getElementById("quantity-" + item.productId);
        if (input) input.value = item.quantity;
    });
}

document.addEventListener("DOMContentLoaded", loadCart);