using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Core.Services;

public sealed class ProfileSectionService
{
    public IReadOnlyList<ProfileSection> BuildSections(
        IReadOnlyList<Scene> scenes,
        IReadOnlyList<Device> devices,
        IReadOnlyDictionary<string, DeviceState> states,
        IReadOnlyList<FavoriteAssignment> favorites,
        IReadOnlyDictionary<string, IReadOnlyList<ConsoleBinding>> bindingsByDevice)
    {
        var sections = new List<ProfileSection>
        {
            new("scenes", "Scenes", scenes.Select(scene => new ProfileActionItem(
                scene.DisplayName,
                "Execute SmartThings scene",
                new ConsoleBinding(ConsoleBindingKind.Scene, scene.Id, scene.DisplayName))).ToArray()),
            new("devices", "Devices", BuildDeviceItems(devices, states, bindingsByDevice, categoryFilter: null)),
            new("favorites", "Favorites", favorites.Select(favorite => new ProfileActionItem(
                favorite.DisplayName,
                "Pinned favorite",
                favorite.Binding)).ToArray()),
            new("laundry", "Laundry", BuildDeviceItems(devices, states, bindingsByDevice, DeviceCategory.Laundry)),
            new("tv-media", "TV / Media", BuildDeviceItems(devices, states, bindingsByDevice, DeviceCategory.Media)),
        };

        return sections;
    }

    private static IReadOnlyList<ProfileActionItem> BuildDeviceItems(
        IReadOnlyList<Device> devices,
        IReadOnlyDictionary<string, DeviceState> states,
        IReadOnlyDictionary<string, IReadOnlyList<ConsoleBinding>> bindingsByDevice,
        DeviceCategory? categoryFilter)
    {
        var items = new List<ProfileActionItem>();

        foreach (var device in devices)
        {
            if (!bindingsByDevice.TryGetValue(device.Id, out var bindings))
            {
                continue;
            }

            foreach (var binding in bindings.Where(binding => categoryFilter is null || binding.Category == categoryFilter))
            {
                states.TryGetValue(device.Id, out var state);
                items.Add(new ProfileActionItem(
                    binding.EffectiveLabel,
                    state?.ErrorMessage ?? "SmartThings device binding",
                    binding));
            }
        }

        return items;
    }
}
