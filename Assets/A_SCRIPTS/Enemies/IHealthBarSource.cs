// Lo que necesita EnemyHealthBar para dibujar la vida.
// Lo implementan EnemyHealth (enemigos) y SpecialTower (torres), así las dos usan la misma barra.
public interface IHealthBarSource
{
    bool IsInitialized { get; }
    float GetMaxHealt();
    float GetHealthNormalized();
}
