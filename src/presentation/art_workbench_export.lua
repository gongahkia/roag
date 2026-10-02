-- Explicit, presentation-only handoff boundary for the standalone
-- `unpolished-bees` project. It copies art source and serializable adapter
-- data only; the sibling tool deliberately receives no App, simulation, save,
-- profile, route, or fallen-archive code.
local ArtPacks = require("src.rendering.art_packs")

local Export = {
  FORMAT = "unpolished_bees.art_workbench",
  VERSION = 2,
  entries = {
    { source = "assets/art_packs", destination = "roag_assets/art_packs", directory = true },
    { source = "assets/kenney", destination = "roag_assets/kenney", directory = true },
    { source = "sprite_editor/colored-transparent_packed.png", destination = "roag_assets/colored-transparent_packed.png" },
    { source = "sprite_editor/mappings.json", destination = "roag_contract/mappings.json" },
    { source = "content/screens/legacy.json", destination = "roag_contract/screens.json" },
    { source = "content/presentation/flow.json", destination = "roag_contract/flow.json" },
    { source = "content/presentation/art_pack.json", destination = "roag_contract/art_pack.json" },
    { source = "content/presentation/art_packs.json", destination = "roag_contract/art_packs.json" },
  },
}

local function safe_relative(path)
  return type(path) == "string" and path ~= "" and not path:match("^/")
    and not path:match("^%.%.$") and not path:match("^%.%./")
    and not path:match("/%.%./") and not path:match("/%.%.$")
end

function Export.validate()
  for _, entry in ipairs(Export.entries) do
    assert(safe_relative(entry.source) and safe_relative(entry.destination), "Art-workbench export contains an unsafe path")
    assert(not entry.source:match("^src/simulation/") and not entry.source:match("^src/persistence/"),
      "Art-workbench export must not contain gameplay or persistence code")
  end
  return true
end

function Export.manifest()
  assert(Export.validate())
  local packs = {}
  for _, pack in ipairs(ArtPacks.list()) do
    packs[#packs + 1] = {
      id = pack.id,
      display_name = pack.display_name,
      license = pack.license,
      credit = pack.credit,
      source_url = pack.source_url,
    }
  end
  return {
    format = Export.FORMAT,
    version = Export.VERSION,
    project_name = "unpolished-bees",
    purpose = "Standalone art-pack catalog and ROAG sprite-mapping workbench.",
    integration = {
      selection_format = "roag.presentation_art_pack",
      selection_version = 1,
      selected_art_pack_key = "art_pack_id",
      settings_path = "content/presentation/art_pack.json",
      flow_path = "content/presentation/flow.json",
      screen_path = "content/screens/legacy.json",
      sprite_mapping_path = "sprite_editor/mappings.json",
      note = "Copy only declared presentation files into ROAG; this never changes an active run, account profile, or fallen archive.",
    },
    art_packs = packs,
  }
end

return Export
