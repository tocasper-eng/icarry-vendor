-- Development sample data. All names and numbers are made up.
-- Every sample account's password is "demo1234" (SHA-1 below).

INSERT INTO vendor (id, name, is_on, shipping_self, service_fee) VALUES
    (1, '示範伴手禮商家', 1, 0, '[{"name":"iCarry","percent":20},{"name":"momo","percent":25}]'),
    (2, '其他商家',       1, 0, '[{"name":"iCarry","percent":18}]'),
    (3, '已停用商家',     0, 0, NULL);

INSERT INTO vendor_account (id, account, pwd, name, vendor_id, shop_id, shop_admin, is_on) VALUES
    (1, 'demo',     '6ea7ccdcf642953a24672d10b0d32cef576e0329', '示範管理員', 1, 0, 1, 1),
    (2, 'disabled', '6ea7ccdcf642953a24672d10b0d32cef576e0329', '停用帳號',   1, 0, 1, 0),
    (3, 'posonly',  '6ea7ccdcf642953a24672d10b0d32cef576e0329', 'POS 帳號',   1, 0, 0, 1),
    (4, 'offvendor','6ea7ccdcf642953a24672d10b0d32cef576e0329', '停用商家帳號', 3, 0, 1, 1),
    (5, 'other',    '6ea7ccdcf642953a24672d10b0d32cef576e0329', '其他商家管理員', 2, 0, 1, 1);

INSERT INTO category (id, name) VALUES (1, '糕餅點心'), (2, '茶葉'), (3, '肉乾');

INSERT INTO parcel_tax_code (hscode, name, unit_name, tax) VALUES
    ('1905', '糕餅、餅乾', '公斤', '15'),
    ('0902', '茶葉',       '公斤', '15');

INSERT INTO product (id, vendor_id, category_id, name, model_name, serving_size, price, service_fee_percent, parcel_tax_code, status, is_del) VALUES
    (101, 1, 1, '鳳梨酥禮盒',   '口味', '12 入',   450, 0,  '1905',  1, 0),
    (102, 1, 1, '太陽餅',       '規格', '6 入',    280, 2,  '1905',  1, 0),
    (103, 1, 2, '高山烏龍茶',   '重量', '150g',    600, -1, '0902', -3, 0),
    (104, 1, 3, '豬肉乾',       '口味', '200g',    320, 0,  NULL,    0, 0),
    (105, 1, 1, '蛋黃酥',       '規格', '8 入',    520, 0,  '1905', -1, 0),
    (106, 1, 2, '東方美人茶',   '重量', '75g',     880, 0,  '0902', -2, 0),
    (107, 1, 1, '牛軋糖',       '口味', '300g',    260, 0,  '1905', -9, 0),
    (108, 1, 1, '已刪除商品',   '規格', '1 入',    100, 0,  NULL,    1, 1),
    (201, 2, 1, '其他商家商品', '規格', '1 入',    199, 0,  NULL,    1, 0);

INSERT INTO product_model (id, product_id, name, sku, quantity, safe_quantity, is_del) VALUES
    (1001, 101, '原味',     'EC00101001', 120, 20, 0),
    (1002, 101, '蔓越莓',   'EC00101002',  15, 20, 0),
    (1003, 102, '原味',     'EC00102001',   0, 10, 0),
    (1004, 103, '150g',     'EC00103001',  30,  5, 0),
    (1005, 104, '原味',     'EC00104001',  50, 10, 0),
    (1006, 105, '原味',     'EC00105001',  10,  5, 0),
    (1007, 106, '75g',      'EC00106001',   8,  2, 0),
    (1008, 107, '花生',     'EC00107001',  60, 10, 0),
    (1009, 107, '已刪規格', 'EC00107002',   0, 10, 1),
    (1010, 108, '原味',     'EC00108001',   0, 10, 0),
    (2001, 201, '原味',     'EC00201001',   0, 10, 0);
