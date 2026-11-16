import csv, math, sys
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
plt.rcParams.update({'font.family':'Liberation Serif','font.size':12,'axes.grid':True,'grid.alpha':0.3,'savefig.dpi':200})
R='results/'; O='fig/'
from matplotlib.ticker import FuncFormatter
_fmt=FuncFormatter(lambda v,p: ('%g'%v).replace('.',','))
_save=matplotlib.figure.Figure.savefig
def _sv(self,*a,**k):
    for ax in self.axes:
        if not getattr(ax,'_cat',False): ax.xaxis.set_major_formatter(_fmt)
        ax.yaxis.set_major_formatter(_fmt)
    return _save(self,*a,**k)
matplotlib.figure.Figure.savefig=_sv
def rd(f): 
    with open(R+f) as h: return list(csv.DictReader(h,delimiter=';'))
def fl(x): return float(x.replace(',','.'))
t=rd('transient.csv'); ts=[fl(r['t'])/1000 for r in t]
fig,ax=plt.subplots(1,2,figsize=(11,4))
for k in range(1,6):
    ax[0].plot(ts,[fl(r[f'T{k}']) for r in t],lw=1,label=f'Прогін {k}')
    ax[1].plot(ts,[fl(r[f'rk{k}']) for r in t],lw=1)
ax[0].set_xlabel('Модельний час, тис. хв'); ax[0].set_ylabel('Середній час обслуговування, хв'); ax[0].legend(fontsize=9)
ax[1].axhline(0.9626,color='k',ls='--',lw=1,label='Теоретичне значення'); ax[1].legend(fontsize=9)
ax[1].set_xlabel('Модельний час, тис. хв'); ax[1].set_ylabel('Завантаження контролерів'); ax[1].set_ylim(0.94,0.98)
fig.tight_layout(); fig.savefig(O+'transient.png'); plt.close()
fig,ax=plt.subplots(1,2,figsize=(11,4))
for k in range(1,6): ax[0].plot(ts[:60],[fl(r[f'T{k}']) for r in t[:60]],lw=1,label=f'Прогін {k}')
for k in range(1,6): ax[1].plot(ts[:60],[fl(r[f'rn{k}']) for r in t[:60]],lw=1)
ax[0].set_xlabel('Модельний час, тис. хв'); ax[0].set_ylabel('Середній час обслуговування, хв'); ax[0].legend(fontsize=9)
ax[1].axhline(0.9626,color='k',ls='--',lw=1); ax[1].set_xlabel('Модельний час, тис. хв'); ax[1].set_ylabel('Завантаження настроювача')
fig.tight_layout(); fig.savefig(O+'transient_start.png'); plt.close()
d=rd('duration.csv')
fig,ax=plt.subplots(figsize=(7,4))
ax.plot([fl(r['tmod'])/1e6 for r in d],[fl(r['std']) for r in d],'o-')
ax.set_xlabel('Тривалість спостереження, млн хв'); ax.set_ylabel('Середньоквадратичне відхилення, хв')
fig.tight_layout(); fig.savefig(O+'duration.png'); plt.close()
for name,xl in [('T','Середній час обслуговування, хв'),('rk','Завантаження контролерів'),('rn','Завантаження настроювача')]:
    h=rd(f'hist_{name}.csv'); lo=[fl(r['lo']) for r in h]; hi=[fl(r['hi']) for r in h]; c=[int(r['count']) for r in h]
    fig,ax=plt.subplots(figsize=(7,4))
    ax.bar(lo,c,width=[b-a for a,b in zip(lo,hi)],align='edge',edgecolor='k',color='#9db9d9')
    runs=rd('baseline_runs.csv'); x=[fl(r[name]) for r in runs]; n=len(x); m=sum(x)/n; s=(sum((v-m)**2 for v in x)/(n-1))**.5
    w=hi[0]-lo[0]; xs=[lo[0]+i*(hi[-1]-lo[0])/200 for i in range(201)]
    ax.plot(xs,[n*w/(s*math.sqrt(2*math.pi))*math.exp(-(v-m)**2/(2*s*s)) for v in xs],'r-',lw=1.5,label='Нормальний закон')
    ax.set_xlabel(xl); ax.set_ylabel('Частота'); ax.legend(fontsize=9)
    fig.tight_layout(); fig.savefig(O+f'hist_{name}.png'); plt.close()
g=rd('regression_levels.csv'); c=rd('regression_coef.csv'); b0,b1=fl(c[0]['b0']),fl(c[0]['b1']); l0,l1=fl(c[1]['b0']),fl(c[1]['b1'])
a=[fl(r['a']) for r in g]; T=[fl(r['T']) for r in g]
fig,ax=plt.subplots(figsize=(7,4.2))
ax.plot(a,T,'o',label='Результати моделювання')
xs=[5.47+i*(9.1-5.47)/300 for i in range(301)]
ax.plot(xs,[b0+b1*(5.2941/v)/(1-5.2941/v) for v in xs],'r-',label='Регресійна модель (4.4)')
ax.plot(xs,[l0+l1*v for v in xs],'g--',lw=1,label='Лінійна модель')
ax.set_xlabel('Середній інтервал надходження, хв'); ax.set_ylabel('Середній час обслуговування, хв'); ax.legend(fontsize=9); ax.set_ylim(0,100)
fig.tight_layout(); fig.savefig(O+'regression.png'); plt.close()
u=rd('mod1_unstable.csv')
fig,ax=plt.subplots(figsize=(7,4))
ax.plot([fl(r['t'])/1000 for r in u],[int(r['L1']) for r in u],'o-',label='Черга до контролерів')
ax.plot([fl(r['t'])/1000 for r in u],[int(r['L2']) for r in u],'s-',label='Черга до настроювача')
ax.set_xlabel('Модельний час, тис. хв'); ax.set_ylabel('Довжина черги, ТВ'); ax.legend(fontsize=9)
fig.tight_layout(); fig.savefig(O+'mod1_unstable.png'); plt.close()
m=rd('modifications.csv')
fig,ax=plt.subplots(figsize=(7,4))
ax._cat=True
ax.bar([r['code'] for r in m],[fl(r['T']) for r in m],yerr=[fl(r['half']) for r in m],capsize=4,color='#9db9d9',edgecolor='k')
for i,r in enumerate(m): ax.text(i,fl(r['T'])+2,f"{fl(r['T']):.1f}".replace('.',','),ha='center')
ax.set_ylabel('Середній час обслуговування, хв'); ax.set_xlabel('Варіант моделі')
fig.tight_layout(); fig.savefig(O+'modifications.png'); plt.close()
print('ok')
