namespace Turbo.Plugins
{
    public enum ItemLocation : int
    {
        Floor = -1,
        Inventory = 0,
        Head = 1,
        Torso = 2,
        RightHand = 3,
        LeftHand = 4,
        Hands = 5,
        Waist = 6,
        Feet = 7,
        Shoulders = 8,
        Legs = 9,
        Bracers = 10,
        LeftRing = 11,
        RightRing = 12,
        Neck = 13,
        MerchantBuyback = 14,
        Stash = 15,
        Gold = 16,
        MerchantAvaibleItemsForPurchase = 17,
        Merchant = 18,
        PtrVendor = 19,
        InSocket = 20,
        PetRightHand = 21,   // Root.NormalLayer.hireling_dialog_mainPage.hireling_slot_offhand
        PetLeftHand = 22,    // Root.NormalLayer.hireling_dialog_mainPage.hireling_slot_mainhand
        PetSpecial = 23,     // Root.NormalLayer.hireling_dialog_mainPage.hireling_slot_special
        PetNeck = 24,        // Root.NormalLayer.hireling_dialog_mainPage.hireling_slot_neck
        PetRightRing = 25,   // Root.NormalLayer.hireling_dialog_mainPage.hireling_slot_right_finger
        PetLeftRing = 26,    // Root.NormalLayer.hireling_dialog_mainPage.hireling_slot_left_finger
        PetHead = 27,        // Root.NormalLayer.hireling_dialog_mainPage.hireling_slot_head
        PetTorso = 28,       // Root.NormalLayer.hireling_dialog_mainPage.hireling_slot_torso
        PetHands = 29,       // Root.NormalLayer.hireling_dialog_mainPage.hireling_slot_hands
        PetWaist = 30,       // Root.NormalLayer.hireling_dialog_mainPage.hireling_slot_waist
        PetFeet = 31,        // Root.NormalLayer.hireling_dialog_mainPage.hireling_slot_feet
        PetShoulders = 32,   // Root.NormalLayer.hireling_dialog_mainPage.hireling_slot_shoulders
        PetLegs = 33,        // Root.NormalLayer.hireling_dialog_mainPage.hireling_slot_legs
        PetBracers = 34,     // Root.NormalLayer.hireling_dialog_mainPage.hireling_slot_bracers
        VendorToken = 1000
    }
}