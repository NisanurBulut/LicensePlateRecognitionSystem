# PlakaTanimaSistemi
Araç Fotoğraflarından plaka tanıma işlemi yapan masaüstü uygulamadır.

 

İçindekiler
1.	Plaka Tanıma Yazılımı Nedir
2.	Plaka Tanıma Yazılımının Hedefleri
3.	Kullanılan Yöntemler
3.1	Gri Seviye İndirgeme
3.2	Median Filtreleme
3.3	Sobel Filtreleme
3.4	Otsu Algoritması
4.	Morfolojik İşlemler
4.1	Aşındırma İşlemi
4.2	Genişletme İşlemi
4.3	Gürültü Yok Etme
4.4	Opening/Closing İşlemi
5.	Aforge Kütüphanesi ve Damla Filtreleme İşlemi   
6.	Filtre Uygulama İşlemlerinin Özeti
7.	Plaka bölgesinin belirlenmesi
8.Özet







1.	Plaka Tanıma Yazılımı Nedir?
Plaka Tanıma Sistemi kameralardan elde edilen araç görüntüsünün üzerinde plaka bölgesi tespit edilerek ve ayrıştırılarak, plaka üzerinde bulunan karakterlerin optik karakter tanıma (Görüntü İşleme) yöntemleri ile okunması işlemidir. Dijital görüntülerin alınması sayesinde plaka okumasının gerçekleştiriliyor olması sayesinde, yazılım tabanlı olarak çalışırlar. Projeye göre değişik uygulamaları olup, uygulamaya özel algoritma ve donanım yapısının bir araya getirilmesi ile oluşturulan bir sistemdir.
2.	Plaka Tanıma Yazılımının Hedefleri

Araçların plaka bilgisinin, görüntülerden elde edilmesiyle aslında aracın kimlik bilgisi elde edilir. Bu sayede bir araca dair aşağıda belirtilen durumlar takip edilebilir.
a)	Otoyol, köprü ve gişelerden geçen araçların çalıntı veya ihlal bilgilerine ulaşabilmek ve analizlerini yapabilmek
b)	Otopark, site, nizamiye ve buna benzer kontrollü geçiş noktalarının giriş ve çıkışlarında insan gücünü hafifletmek, kolay, hızlı, güvenli giriş ve çıkışlarını sağlamak
c)	Kantar, araç muayene istasyonları ve buna benzer yerlerde plaka bilgilerine otomatik olarak ulaşabilmek
d)	Şehir giriş ve çıkışlarının kontrol altında tutulması, şehir güvenliğinin sağlanması
e)	Karayollarının araç yoğunluk ölçümlerinin yapılması
f)	Karayollarının stratejik ulaşım analizlerinin yapılması
g)	Trafik düzenleme işlemleri
h)	İki nokta arasında araçların ulaşım süresinin belirlenmesi, bilgi panosuna aktarılması
i)	Akıllı trafik sistemlerinin kurulması
j)	Otopark kontrolü, yoğunluk ölçümleri
k)	Kayıtlı olan ve ya geçiş yetkisi bulunan araçlar için otomatik bariyer devreye girmesi ya da yasaklı, izinsiz araçların geçmeye çalışma durumunda alarmın devreye girmesi

3.	Kullanılan Yöntemler

Aracın plaka bilgisinin elde edilebilmesi için plakanın koordinat bilgisi bilinmelidir.Kordinatların yer tespitinin ardından karakter tanıma işlemi yapılır.  Genel anlamda süreç adım adım ifade edilecek olursa aşağıdaki gibi listelenir.

a)	Kamera görüntüsü üzerinden plaka yerinin tespit edilmesi ve ayrıştırılması
b)	Plakanın sonraki algoritmalara uygun şekilde yeniden konumlandırılması ve boyutlandırılması
c)	Parlaklık, zıtlık gibi görüntü özelliklerinin normalizasyonu
d)	Karakter ayırma ile görüntüden karakterlerin çıkarılması
e)	Optik karakter tanıma
f)	Ülkeye özgü söz dizimi ve geometrik kontroller

3.1	Gri Seviye İndirgeme

Uygulama sürecinde görüntü işleme hızını artırabilmek için RGB formattaki görüntü, ortalama değer yöntemiyle gri seviyeye indirgenmiştir. Byte veri tipi dönüşümü yapılır. 

	Resim üzerinde yapılan her işlemin tamamlanma süreci, progressbar ile gösterilir.
![Resim 1- Gri Seviye İndirgeme C# Kod Bloğu](https://github.com/NisanurBulut/PlakaTanimaSistemi/blob/master/PlakaTanimaSistemi/ProjeTanitimImages/1.png)
 
![Resim2-Gri Seviye İndirgeme Sonucu](https://github.com/NisanurBulut/PlakaTanimaSistemi/blob/master/PlakaTanimaSistemi/ProjeTanitimImages/2.jpg)
 
3.2	Median Filtreleme

Gri seviyeye indirgenmiş olan görüntü üzerinde keskin geçişleri en az seviyeye indirmek için median filtre uygulanacaktır. Median filtre 3x3, 5x5, 7x7 gibi tek sayı boyutlu filtrelerden oluşur. Görüntüyü yumuşatır. Kullanılan çekirdek şablonun yani filtrenin boyutu arttıkça yumuşama yani bulanıklaşma da artar.
Median filtrenin tercih edilme sebebi ortalama alıcı filtreyle kıyaslandığında daha sağlıklı sonuçlar vermesidir. Görüntü üzerindeki detay kaybı daha az olur. Bir pikselin değerini değiştirirken komşularının ve kendisinin ortalamasını almak yerine komşuları içinde ortanca değer ile değişim yapar.
Temsil yeteneği uzak bir piksel sıralanan dizinin uçlarında kalacağından (hiç bir zaman ortada bulunmayacaktır) oradaki komşuların genel temsilini etkilemesi imkansız hale gelmiş olur.
Komşu piksellerin birinin değeri olması gerektiği için, kenar boyunca hareket ettiğinde gerçekçi olmayan piksel değerleri oluşturmaz. Bu nedenle, medyan filtre, keskin kenarları ortalama filtreden daha iyi korur
Filtreleme işlemleri ExtBitmap C# sınıfı içerisinde tanımlanmıştır. 3x3 Matrix boyutu kullanılmıştır

![Resim3-ExtBitmap sınıfı genel görünümü](https://github.com/NisanurBulut/PlakaTanimaSistemi/blob/master/PlakaTanimaSistemi/ProjeTanitimImages/3.png)

![Resim4- Median filtrenin uygulaması](https://github.com/NisanurBulut/PlakaTanimaSistemi/blob/master/PlakaTanimaSistemi/ProjeTanitimImages/4.jpg)


3.3	Sobel Filtreleme

Median filtre uygulanarak gürültüsü azaltılmış, keskin geçişler azaltılacaktır.  Görüntü üzerinde kenar bulma işlemi için sobel filtresi kullanılacaktır. Dikey, yatay ve köşegen şeklindeki kenarları bulmak için kullanılacaktır.  

![](https://github.com/NisanurBulut/PlakaTanimaSistemi/blob/master/PlakaTanimaSistemi/ProjeTanitimImages/sobel.JPG)


![Resim5-Sobel filtrelerinin uygulama içerisinde gösterimi](https://github.com/NisanurBulut/PlakaTanimaSistemi/blob/master/PlakaTanimaSistemi/ProjeTanitimImages/5.png)	 

![Resim5-Sobel filtrelerisinin uygulaması](https://github.com/NisanurBulut/PlakaTanimaSistemi/blob/master/PlakaTanimaSistemi/ProjeTanitimImages/6.png)	 
 



3.4	Otsu Algoritması

Gri seviyeli görüntünün ikili seviyeye dönüştürülebilmesi için otsu algoritması kullanılacaktır. İkili seviyeye dönüştürmek için bir eşik değeri belirlenir. Bu eşik değerinin altında kalan pikseller siyaha dönüştürülürken üstünde kalan pikseller beyaza dönüştürülür. Ancak her görüntü aynı niteliklere sahip değildir dolayısıyla her görüntünün kendine has eşik değerinin hesaplanmasına ihtiyaç duyulur. Uygulamanın bu adımında, eşik değerinin tespit edilmesi için Otsu Algoritması kullanılacaktır.
Otsu Algortiması eşik değer yöntemi mümkün olan bütün eşik değerler için(255'e kadar yani) bütün eşik değerler için sınıf-içi varyans denilen bir değer hesaplar ve bu değerin en düşük olduğu indeksi döndürür. Sınıf-içi varyansın minimize edilmesi derken kast edilen sınıflar önyüz(foreground) ve arkayüz(background) pikselleridir. O an incelenen renk değerinden büyük olan piksellere önyüz pikselleri; küçük olanlar arkayüz pikselleri denir. Bir renk için histogramda(buckets) o renkten büyük olan renklerin([i:]) frekanslarının  toplamının(np.sum) toplam piksel sayısına(image_size) bölümü bize önyüz renklerinin ağırlığını verir. Bu değerin 1 sayısından çıkarılmasıyla arkayüz ağırlığı elde edilir.
Varyans hesabı yapabilmek için önce sınıfların ortalama değerleri bulunmalıdır. Bunun için her sınıf için o sınıfa üye olan renklerin kendi değerleri ile o renkteki piksel sayısı çarpılır ve sonuç, kümeye ait toplam piksel sayısına bölünür.
Otsu algoritmasının kullanımı için .dll dosyasından yararlanılmıştır. Bu method birebir proje içerisinde kodlanmamıştır.

![Resim6-Otsu algoritması için  dll dosyası çağrısı.](https://github.com/NisanurBulut/PlakaTanimaSistemi/blob/master/PlakaTanimaSistemi/ProjeTanitimImages/6.jpg)

![Resim7-otsu algoritması çalıştırılması](https://github.com/NisanurBulut/PlakaTanimaSistemi/blob/master/PlakaTanimaSistemi/ProjeTanitimImages/otsu2.jpg)


 ![Resim8-Otsu algoritması uygulaması](https://github.com/NisanurBulut/PlakaTanimaSistemi/blob/master/PlakaTanimaSistemi/ProjeTanitimImages/otsu3.jpg)


4.	Morfolojik İşlemler

Görüntü üzerinde iskelet, imgedeki sınırlar gibi yapıların tanımlanması ve bilgi çıkarımı yapılması ve gürültü giderimi, bölütleme için matematiksel morfoloji işlemlerine ihtiyaç vardır. Morfolojik görüntü işleme şekillerin biçimsel yapısı ile ilgilenerek nesneleri ayırt etmemize ve gruplayabilmemize olanak sağlar. Yöntem gri seviye görüntüler üzerinde de çalışsa da genellikle siyah-beyaz (ikili) görüntüler üzerinde kullanılır. Morfolojik filtreler genelde iki temel işlemden türetilmiştir. Bunlar erosion (aşındırma) ve dilation (genişletme) işlemleridir. Aşındırma ikili bir görüntüde bulunan nesnelerin boyutunu seçilen yapısal elemente bağlı olarak küçültürken, genişletme nesnenin alanını artırır.

	Morfolojik işlemler enum olarak kodlama içerisinde yer almıştır.
 

	Uygulama için otsu görüntü üzerinde morfolojik işlemler olan yayma ve aşındırma işlemleri yapılacaktır.
 
 Resim9-Dilation ve Erosion İşlemlerinin Extmap sınıfından çağrılması

  
4.1	Aşındırma İşlemi(Erosion)
Bu işlem için görüntü üzerinde n boyutlu bir çekirdek gezdirilecektir. Ortadaki n. piksel, resim üzerinde işlem yaptığımız piksele karşılık gelir. Bu n tane piksel resim üzerine konulduktan sonra, piksellerin tamamı beyaz alanla örtüştü ise yani, n tane pikselin hepsinin karşılığı olan alan beyaz ise o zaman üzerinde işlem yapılan piksel beyaz olarak işaretlenir. Eğer bu n tane pikselden herhangi biri siyah bir pikselin üzerine denk geldiyse o zaman ortadaki pikselin değeri siyah yapılır. Dikkat edilirse bu işlem ile siyah bölge genişletilirken, beyaz bölge aşındırılmış olmaktadır.
	Uygulama için birbirine ince gürültülerle bağlanmış nesneleri birbirinden ayırarak gürültü temizleme yapılmak amaçlanmıştır.
 
Resim9-Erozyon işleminin uygulanması
4.2	Genişletme İşlemi(Dilation)

Genişletme işlemi aynı nesnenin bir gürültü ile ince bir şekilde bölünerek ayrı iki nesne gibi görünmesini engellemek için kullanılır. Aslında aşındırma ve genişletme işlemleri birbirinin tersidir. Görüntü üzerindeki alanlarda bu işlemlerden birini uygulandığında komşu diğer alanlar zıttı olan işleme tabi tutulmuş olur. Yani aşındırma uygularken komşu alanda genişletme uygulanmış olur.

	Uygulama için genişletme işlemi aynı nesnenin bir gürültü ile ince bir şekilde bölünerek ayrı iki nesne gibi görünmesini engellemek için kullanılır.
 
Resim10-Dilation İşlemi
4.3	Opening/Closing İşlemi
Aşındırma ve genişletme işlemlerinin ardından  sırasıyla opening ve closing işlemleri yapılır. Buradaki amaç detayları ele geçirmektir. Kodlama aşamasında opening ve closing işlemlerinin yapıldığı methodlar Extmap sınıfı içinde tanımlanmıştır.

 
Resim11-Opening&Closing işleminin çağrısı
 
Resim 12-Closing işlem uygulamasının sonucu

4.4	Gürültü Yok etme

Görüntü üzerinde filtreleme işlemlerinin ve morfolojik işlemlerin yapılmasının ardından kalan gürültüleri temizlemek için Aforge kütüphanesinden yararlanıldı. Görüntü üzerinde ﬁltreleme ve morfolojik işlemlerimizi yaptıktan sonra görüntü üzerinde kalan görültüleri temizlemek için AForge kütüphanesin BlobsFiltering yani damla ﬁltreleme işlemini uygulayarak verilen boyutlar dışında kalan bölgeleri temizledik. 

	Filtre genişliği 70 pixel, yüksekliği 40 pixel den küçük olan gürültüleri yok etmek için kullanılmıştır.
	Üzerinde işlem yapılan görüntüde kalan noktasal gürültülerden kurtulmak amaçlanır.
 
Resim13-Gürültü temizleme kod parçacığı
 
Resim14-Gürültü temizleme işlem sonucu

5.	Aforge Kütüphanesi ve Damla Filtreleme İşlemi

Aforge kütüphanesi görüntü işleme alanında kullanılan açık kaynak kodlu bir  .NET kütüphanesidir. Görüntü üzerinde manuel olarak ya da otomatik olarak matematik işlemler yapılmasına olanak sağlar. Filtrelemeden sinir ağları hesaplamalarına değin pek çok alanda kolaylıklar sağlar.

	Uygulama içinde, Aforge kütüphanesi yardımıyla gürültü temizleme işlemi için damla filtre metodu kullanılacaktır.
Damla filtre yardımıyla, verilen boyutlardan küçük olan gürültüler temizlenir. Kütüphane bunun için BlobsFiltering metodunu kullanıma sunar.
 
Resim11-Dilation uygulanan resme damla filtresinin uygulanması
 
Resim 12-Damla filtresinin resim üzerindeki etkisi
6.	Filtre Uygulama İşlemlerinin Özeti
Aforge kütüphanesinin yardımıyla, otsu algoritması için kullanılan dll dosyasının yardımıyla ve sobel, median filtrelerinin tanımlayıcı olarak kodlanmasıyla ardarda pek çok filtreleme işlemi yapılmıştır. Yapılan işlemler özetle sıralanacak olursa aşağıdaki gibidir. Ardarda pek çok filtreleme işlemi yaparak plaka görüntüsünün elde edilmesine uygun bir taslak hazırlanmaya çalışılmıştır.

7.	Plaka Bilgisinin Araştırılması
Görüntü üzerinde gürültü temizleme ve filtreleme işlemlerinin yapılmasının ardından plaka yerinin araştırılması işlemine geçilir. Bunun için Aforge kütüphanesinden yararlanılır ve plakanın koordinat bilgileri elde edilmeye çalışılacaktır.
Türkiye’deki araç plakalarının birçoğunun görünümü dikdörtgen şeklindedir. Aforge kütüphanesi yardımıyla elde edilen plaka bölgesinin gerçekten plaka boyutlarına uygun olup olmadığını anlayabilmek için boyut kontrolü yapılmalıdır. Plaka boyutları ön taraf için genellikle 11x52 iken arka taraf için 21x32’dir. Sonuç olarak elde edilen görüntü plaka bilgisi olarak kullanıcıya sunulacaktır.

	Uygulama sürecinde plaka tespiti yalnızca aracın ön tarafındaki plaka için yapılmıştır.
 
Resim13-Plaka bölgesinin çizimi

 
Resim14- Plaka bölgesinin çizilmesi için gerekli kod bloğu
 
Resim15-Plaka bölge sınırlarının belirlenmesi için gerekli kod bloğu
 
Resim16-Plaka bölgesinin dikdörtgen şekilde gösterimi

8.	Özet
Görüntünün gri seviyeye indirgenmesi, yumuşatma işlemi, kenar bulma filtesinin(sobel) kullanımı, aşındırma ve genişletme işlemlerinin yapılmasının ardından Aforge kütüphanesi yardımıyla plaka bölgesinin araştırılması ve belirlenmesi gerçekleştirilmiştir.
	Proje C# programlama dili kullanılarak masaüstü uygulaması olarak gerçekleştirilmiştir.

Programın Kullanım Adımları

a)	Kullanıcı Dosya seç butonunu kullanarak, plakasını tanımak istediği fotoğrafın fiziksel yolunu programa tanıtır.
b)	Fotoğrafın yüklenmesinin ardından, plaka tespit butonu tıklanarak plaka tanıma işlemi başlatılır.
c)	Plaka tanıma işlemi tamamlandığında, kullanıcıya plaka gösterimi yapılır.
 	 
Program başlangıç ekranı	Fotoğrafın programa tanıtılması
 	 
Plaka tespit aşaması	Plaka tespitinin tamamlanması
Tablo1-Programın çalışma sürecinin gösterilmesi
Filtre işlemlerinin gösterimi
 	 
 	 
 	 
 	 
 	 
 	 
 	 


Proje Kaynak Kodu: https://github.com/NisanurBulut/PlakaTanimaSistemi


Kaynakça
1.	https://trafik.net.tr/plaka-tanima-sistemi-nedir/
2.	http://www.hobibilisim.com/pts-nedir/
3.	http://www.barissamanci.net/Makale/20/goruntu-isleme-teknikleriyle-gercek-zamanli-plaka-tanima-sistemi/
4.	http://www.aforgenet.com/framework/docs/html/4a83d944-d776-ba2c-9847-3254fe3dbfdd.htm
5.	https://www.bulentsiyah.com/goruntunun-sinir-egrisini-cikaran-filtreler-matlab/
6.	https://yavuzbugra.wordpress.com/2011/05/01/goruntu-islemede-filtreleme/
7.	http://bilgisayarkavramlari.sadievrenseker.com/2007/11/26/ortanca-filitresi-median-filter/
8.	http://bilgisayarkavramlari.sadievrenseker.com/2008/11/17/aritmetik-ortalama-average-mean/
9.	http://huseyinatasoy.com/Otsu-Esik-Belirleme-Metodu
10.	http://embedded.kocaeli.edu.tr/otsu-metodu/
11.	https://medium.com/@sddkal/python-ile-g%C3%B6r%C3%BCnt%C3%BC-i%CC%87%C5%9Fleme-mean-ve-median-filtreler-1891cdbef632
12.	http://www.plaka.com.tr/plaka-gorunumu.html
13.	http://guraysonugur.aku.edu.tr/wp-content/uploads/sites/22/2017/05/G%C3%B6r%C3%BCnt%C3%BC-%C4%B0%C5%9Fleme-Ders-9.1.pdf
14.	https://slideplayer.biz.tr/slide/10376908/
15.	http://www.cescript.com/2012/08/morfolojik-goruntu-isleme.html



