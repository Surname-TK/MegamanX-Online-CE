namespace MMXOnline;

public enum PickupType {
	HeartTank,
	SubTank,
	Health,
	Ammo
}

public enum PickupTypeRpc {
	HeartTank,
	SubTank,
	LargeHealth,
	SmallHealth,
	LargeAmmo,
	SmallAmmo
}

public class Pickup : Actor {
	public float healAmount = 0;
	public PickupType pickupType;
	public int getMaxHeartTanks() {
		return Global.level.server?.customMatchSettings?.maxHeartTanks ?? 8;
	}
	public int getMaxSubTanks() {
		return Global.level.server?.customMatchSettings?.maxSubTanks ?? 4;
	}
	public Pickup(Player owner, Point pos, string sprite, ushort? netId, bool ownedByLocalPlayer, NetActorCreateId netActorCreateId, bool sendRpc = false) :
		base(sprite, pos, netId, ownedByLocalPlayer, false) {
		netOwner = owner;
		collider.wallOnly = true;
		collider.isTrigger = false;

		this.netActorCreateId = netActorCreateId;
		if (sendRpc) {
			createActorRpc(owner.id);
		}
	}

	public override void update() {
		base.update();
		var leeway = 500;
		if (ownedByLocalPlayer && pos.x > Global.level.width + leeway || pos.x < -leeway || pos.y > Global.level.height + leeway || pos.y < -leeway) {
			destroySelf();
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		var player = Global.level.mainPlayer;
		if (other.otherCollider.flag == (int)HitboxFlag.Hitbox) return;

		if (other.gameObject is Character chr) {
			if (!chr.ownedByLocalPlayer) return;

			if (pickupType == PickupType.HeartTank) {
				if (player.heartTanks >= getMaxHeartTanks()) return;
					player.heartTanks++;
					Global.playSound("hearthX1");
					float currentMaxHp = player.maxHealth;
					player.maxHealth = player.getMaxHealth();
					player.character?.addHealth(player.maxHealth - currentMaxHp);
					destroySelf(doRpcEvenIfNotOwned: true);
			} else if (pickupType == PickupType.SubTank) {
				if (player.subtanks.Count >= getMaxSubTanks()) return;
					player.subtanks.Add(new SubTank());
					Global.playSound("hearthX1");
					destroySelf(doRpcEvenIfNotOwned: true);
			} else if (pickupType == PickupType.Health) {
				if (chr.player.health >= chr.player.maxHealth && !chr.player.hasSubtankCapacity()) return;
					chr.addHealth(healAmount);
					destroySelf(doRpcEvenIfNotOwned: true);
			} else if (pickupType == PickupType.Ammo) {
				if (chr.canAddAmmo()) {
					chr.addAmmo(healAmount);
					destroySelf(doRpcEvenIfNotOwned: true);
				}
			}
		} else if (other.gameObject is RideArmor rideArmor) {
			if (!rideArmor.ownedByLocalPlayer) return;

			if (rideArmor.character != null) {
				if (pickupType == PickupType.Health) {
					if (rideArmor.health >= rideArmor.maxHealth) {
						if (rideArmor.character != null && (
							rideArmor.character.player.health >= rideArmor.character.player.maxHealth
						)) {
							return;
						} else {
							rideArmor.character?.addHealth(healAmount);
						}
					} else {
						rideArmor.addHealth(healAmount);
					}
					destroySelf(doRpcEvenIfNotOwned: true);
				} else if (pickupType == PickupType.Ammo) {
					//rideArmor.character.addAmmo(this.healAmount);
					//this.destroySelf();
				}
			}
		} else if (other.gameObject is RideChaser rideChaser) {
			if (!rideChaser.ownedByLocalPlayer) return;

			if (rideChaser.character != null) {
				if (pickupType == PickupType.Health) {
					if (rideChaser.health >= rideChaser.maxHealth) {
						if (rideChaser.character != null &&
							rideChaser.character.player.health >= rideChaser.character.player.maxHealth
						) {
							return;
						} else {
							rideChaser.character?.addHealth(healAmount);
						}
					} else {
						rideChaser.addHealth(healAmount);
					}
					destroySelf(doRpcEvenIfNotOwned: true);
				}
			}
		} else if (other.gameObject is Maverick maverick && maverick.ownedByLocalPlayer) {
			if (pickupType == PickupType.Health &&
				(maverick.health < maverick.maxHealth || maverick.netOwner.hasSubtankCapacity())
			) {
				maverick.addHealth(healAmount, true);
				destroySelf(doRpcEvenIfNotOwned: true);
			} else if (pickupType == PickupType.Ammo && maverick.ammo < maverick.maxAmmo) {
				maverick.addAmmo(healAmount);
				destroySelf(doRpcEvenIfNotOwned: true);
			}
		}
	}
}

public class HeartTankPickup : Pickup {
	public HeartTankPickup(
		Player owner, Point pos, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_hearttank", netId, ownedByLocalPlayer,
		NetActorCreateId.HeartTank, sendRpc: sendRpc
	) {
		healAmount = 0;
		pickupType = PickupType.HeartTank;
	}
}

public class SubTankPickup : Pickup {
	public SubTankPickup(
		Player owner, Point pos, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_subtank", netId, ownedByLocalPlayer,
		NetActorCreateId.SubTank, sendRpc: sendRpc
	) {
		healAmount = 0;
		pickupType = PickupType.SubTank;
	}
}

public class LargeHealthPickup : Pickup {
	public LargeHealthPickup(
		Player owner, Point pos, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_health_large", netId, ownedByLocalPlayer,
		NetActorCreateId.LargeHealth, sendRpc: sendRpc
	) {
		healAmount = 8;
		pickupType = PickupType.Health;
	}
}

public class SmallHealthPickup : Pickup {
	public SmallHealthPickup(
		Player owner, Point pos, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_health_small", netId, ownedByLocalPlayer,
		NetActorCreateId.SmallHealth, sendRpc: sendRpc
	) {
		healAmount = 2;
		pickupType = PickupType.Health;
	}
}

public class LargeAmmoPickup : Pickup {
	public LargeAmmoPickup(
		Player owner, Point pos, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_ammo_large", netId, ownedByLocalPlayer,
		NetActorCreateId.LargeAmmo, sendRpc: sendRpc
	) {
		healAmount = 8;
		pickupType = PickupType.Ammo;
	}
}

public class SmallAmmoPickup : Pickup {
	public SmallAmmoPickup(
		Player owner, Point pos, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_ammo_small", netId, ownedByLocalPlayer,
		NetActorCreateId.SmallAmmo, sendRpc: sendRpc
	) {
		healAmount = 2;
		pickupType = PickupType.Ammo;
	}
}
