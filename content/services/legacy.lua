-- Services are declarative identities.  Stock and transactions are authored
-- by the economy simulation so content never executes behavior.
return {
  {
    id = "service.supply.legacy", display_name = "Supply Kiosk", role = "supply", stock_profile = "legacy_floor", render_style = "supply",
    stock = {
      normal = { offers = {
        { key = "ammo", label = "AMMO", price = 2, remaining = 2 },
        { key = "bombs", label = "BOMB", price = 5, remaining = 1 },
        { key = "flares", label = "FLARE", price = 3, remaining = 1 },
      } },
      -- The hub prepares a player for Apex plus a terminal boss, but cannot
      -- affordably erase all resource decisions at once.
      final_hub = { offers = {
        { key = "ammo", label = "AMMO", price = 2, remaining = 6 },
        { key = "bombs", label = "BOMB", price = 5, remaining = 2 },
        { key = "flares", label = "FLARE", price = 3, remaining = 2 },
      } },
    },
  },
  {
    id = "service.repair.legacy", display_name = "Repair Kiosk", role = "repair", stock_profile = "legacy_floor", render_style = "repair",
    stock = {
      normal = { price = 2, remaining = 2 },
      -- Repairs restore one integrity each. Four hub operations can stabilise
      -- a battered build without making the finale a full reset.
      final_hub = { price = 3, remaining = 4 },
    },
  },
  {
    id = "service.salvager.legacy", display_name = "Salvager", role = "salvager", stock_profile = "legacy_floor", render_style = "salvager",
    stock = { normal = { offer_count = 2 }, final_hub = { offer_count = 4 } },
  },
  {
    id = "service.charm_vendor.legacy", display_name = "Charm Vendor", role = "charm_vendor", stock_profile = "legacy_floor", render_style = "charms",
    stock = { normal = { offer_count = 3 }, final_hub = { offer_count = 5 } },
  },
}
