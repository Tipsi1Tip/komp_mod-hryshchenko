import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle, Circle

plt.rcParams.update({'font.family': 'Liberation Serif', 'font.size': 13, 'mathtext.fontset': 'stix'})
LW = 1.2


def line(ax, pts, arrow=False):
    xs, ys = zip(*pts)
    ax.plot(xs, ys, 'k-', lw=LW)
    if arrow:
        (x0, y0), (x1, y1) = pts[-2], pts[-1]
        ax.annotate('', xy=(x1, y1), xytext=(x0 + (x1 - x0) * 0.6, y0 + (y1 - y0) * 0.6),
                    arrowprops=dict(arrowstyle='-|>', color='k', lw=LW, mutation_scale=14))


def queue(ax, x, y, label=None):
    w, h = 0.55, 0.8
    ax.add_patch(Rectangle((x, y - h / 2), w, h, fill=False, lw=LW))
    for i in range(1, 9):
        xx = x + 0.12 + i * 0.045
        ax.plot([xx, xx], [y - h / 2 + 0.08, y + h / 2 - 0.08], 'k-', lw=0.8)
    if label:
        ax.text(x + w / 2, y + h / 2 + 0.15, label, ha='center', va='bottom')
    return x + w


def device(ax, x, y, name, label=None, below=True):
    ax.add_patch(Circle((x, y), 0.32, facecolor='#d9d9d9', edgecolor='k', lw=LW))
    ax.text(x, y, name, ha='center', va='center')
    if label:
        ax.text(x, y - 0.45 if below else y + 0.45, label, ha='center', va='top' if below else 'bottom')


def text(ax, x, y, s, **kw):
    ax.text(x, y, s, **kw)


def base(fname):
    fig, ax = plt.subplots(figsize=(11, 5.2))
    line(ax, [(0, 3), (2.2, 3)], True)
    text(ax, 0.1, 3.15, r'$t$ = unif(5,5; 2)')
    qx = queue(ax, 2.2, 3, 'СМО1')
    line(ax, [(qx, 3), (3.1, 3), (3.1, 3.8), (3.68, 3.8)])
    line(ax, [(3.1, 3), (3.1, 2.2), (3.68, 2.2)])
    device(ax, 4.0, 3.8, r'$D_{11}$')
    device(ax, 4.0, 2.2, r'$D_{12}$', r'$t$ = unif(9; 3)')
    line(ax, [(4.32, 3.8), (4.9, 3.8), (4.9, 2.2), (4.32, 2.2)])
    line(ax, [(4.9, 3), (8.6, 3)], True)
    text(ax, 7.2, 3.12, '0,85')
    text(ax, 8.7, 2.92, r'$N_{вих}$ (упакування)')
    line(ax, [(5.6, 3), (5.6, 1.0), (6.2, 1.0)], True)
    text(ax, 5.68, 1.9, '0,15')
    qx2 = queue(ax, 6.2, 1.0, 'СМО2')
    line(ax, [(qx2, 1.0), (7.38, 1.0)])
    device(ax, 7.7, 1.0, r'$D_{2}$', r'$t$ = unif(30; 10)')
    line(ax, [(8.02, 1.0), (8.6, 1.0), (8.6, -0.2), (1.6, -0.2), (1.6, 3)])
    line(ax, [(1.6, 2.4), (1.6, 3.0)], True)
    ax.set_xlim(-0.1, 11.2); ax.set_ylim(-0.6, 4.6); ax.axis('off')
    ax.set_aspect('equal'); fig.savefig(fname, dpi=200, bbox_inches='tight', pad_inches=0.05); plt.close(fig)


def mods(fname):
    fig, ax = plt.subplots(figsize=(11, 6))
    line(ax, [(0, 3), (2.4, 3)], True)
    text(ax, 0.05, 3.15, r'$t$ = unif(5,5; 2)')
    text(ax, 1.2, 2.65, '1/k (мод. 1)', ha='center')
    qx0 = queue(ax, 2.4, 3, 'СМО0')
    line(ax, [(qx0, 3), (3.48, 3)])
    device(ax, 3.8, 3, r'$D_{0}$', r'$t$ = 5')
    line(ax, [(4.12, 3), (5.2, 3)], True)
    qx = queue(ax, 5.2, 3, 'СМО1')
    line(ax, [(qx, 3), (6.1, 3), (6.1, 3.8), (6.68, 3.8)])
    line(ax, [(6.1, 3), (6.1, 2.2), (6.68, 2.2)])
    device(ax, 7.0, 3.8, r'$D_{11}$')
    device(ax, 7.0, 2.2, r'$D_{12}$', r'$t$ = unif(9; 3)')
    line(ax, [(7.32, 3.8), (7.9, 3.8), (7.9, 2.2), (7.32, 2.2)])
    line(ax, [(7.9, 3), (11.6, 3)], True)
    text(ax, 9.9, 3.12, r'$1-p$')
    text(ax, 11.7, 2.92, r'$N_{вих}$')
    line(ax, [(8.6, 3), (8.6, 0.8), (9.2, 0.8)], True)
    text(ax, 8.68, 2.0, r'$p$ = 0,15' + '\n(мод. 3: 0,05)', fontsize=12)
    qx2 = queue(ax, 9.2, 0.8, 'СМО2')
    line(ax, [(qx2, 0.8), (10.28, 0.8)])
    device(ax, 10.6, 0.8, r'$D_{2}$', r'$t$ = unif(30; 10)')
    line(ax, [(10.92, 0.8), (12.6, 0.8)], True)
    text(ax, 11.25, 0.92, r'$q$ = 0,4', fontsize=12)
    text(ax, 12.7, 0.72, r'$N_{вих}$')
    line(ax, [(12.0, 0.8), (12.0, -0.6), (4.7, -0.6), (4.7, 3)])
    line(ax, [(4.7, 2.4), (4.7, 3.0)], True)
    text(ax, 8.4, -0.52, r'$1-q$ (мод. 2: 0,6)', ha='center')
    ax.set_xlim(-0.1, 13.6); ax.set_ylim(-0.9, 4.8); ax.axis('off')
    ax.set_aspect('equal'); fig.savefig(fname, dpi=200, bbox_inches='tight', pad_inches=0.05); plt.close(fig)


if __name__ == '__main__':
    import sys
    out = sys.argv[1] if len(sys.argv) > 1 else 'fig/'
    base(out + 'network.png')
    mods(out + 'network_mod.png')
