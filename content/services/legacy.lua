-- Services are declarative identities.  Stock and transactions are authored
-- by the economy simulation so content never executes behavior.
return {
  { id = "service.supply.legacy", display_name = "Supply Kiosk", role = "supply", stock_profile = "legacy_floor", render_style = "supply" },
  { id = "service.repair.legacy", display_name = "Repair Kiosk", role = "repair", stock_profile = "legacy_floor", render_style = "repair" },
  { id = "service.salvager.legacy", display_name = "Salvager", role = "salvager", stock_profile = "legacy_floor", render_style = "salvager" },
  { id = "service.charm_vendor.legacy", display_name = "Charm Vendor", role = "charm_vendor", stock_profile = "legacy_floor", render_style = "charms" },
}
